using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Google.Protobuf;
using Tecan.Sila2;
using Tecan.Sila2.DynamicClient;

namespace Ares.Core.Tests;

[TestFixture]
public class SilaDataConverterTests
{
  [Test]
  public void ToSilaDataType_NumberSchemaWithRange_UsesConstrainedReal()
  {
    var schema = AresSchemaBuilder.Entry(AresDataType.Number)
      .WithNumberRange(0, 5.5)
      .Build();

    var silaType = SilaDataConverter.ToSilaDataType(schema);

    Assert.That(silaType.Item, Is.TypeOf<ConstrainedType>());
    var constrained = (ConstrainedType)silaType.Item;
    using(Assert.EnterMultipleScope())
    {
      Assert.That(constrained.DataType.Item, Is.EqualTo(BasicType.Real));
      Assert.That(constrained.Constraints.MinimalInclusive, Is.EqualTo("0"));
      Assert.That(constrained.Constraints.MaximalInclusive, Is.EqualTo("5.5"));
    }
  }

  [Test]
  public void ToAresValueSchema_SilaConstrainedStringList_PreservesChoices()
  {
    var silaType = new DataTypeType
    {
      Item = new ListType
      {
        DataType = new DataTypeType
        {
          Item = new ConstrainedType
          {
            DataType = new DataTypeType { Item = BasicType.String },
            Constraints = new Constraints
            {
              Set = ["A", "B"]
            }
          }
        }
      }
    };

    var schema = SilaDataConverter.ToAresValueSchema(silaType);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(schema.Type, Is.EqualTo(AresDataType.StringArray));
      Assert.That(schema.AvailableChoicesCase, Is.EqualTo(AresValueSchema.AvailableChoicesOneofCase.StringChoices));
      Assert.That(schema.StringChoices.Strings, Is.EqualTo(new[] { "A", "B" }));
    }
  }

  [Test]
  public void ToSilaProperty_MixedAresList_UsesAnyTypedElements()
  {
    var value = AresValueHelper.CreateList(
    [
      AresValueHelper.CreateString("alpha"),
      AresValueHelper.CreateNumber(42)
    ]);

    var property = SilaDataConverter.ToSilaProperty("Items", value);

    Assert.That(property.Type.Item, Is.TypeOf<ListType>());
    var listType = (ListType)property.Type.Item;
    Assert.That(listType.DataType.Item, Is.EqualTo(BasicType.Any));

    var items = ((IEnumerable<object>)property.Value!).ToList();
    Assert.That(items, Has.Count.EqualTo(2));
    using(Assert.EnterMultipleScope())
    {
      Assert.That(items[0], Is.TypeOf<DynamicObjectProperty>());
      Assert.That(((DynamicObjectProperty)items[0]).Type.Item, Is.EqualTo(BasicType.String));
      Assert.That(((DynamicObjectProperty)items[1]).Type.Item, Is.EqualTo(BasicType.Real));
    }
  }

  [Test]
  public void ToAresValue_UnmarkedQuantityShapedStructure_RemainsStruct()
  {
    var property = new DynamicObjectProperty(
      "Quantity",
      "Quantity",
      null,
      new DataTypeType
      {
        Item = new StructureType
        {
          Element =
          [
            new SiLAElement
            {
              Identifier = "Scalar",
              DataType = new DataTypeType { Item = BasicType.Real }
            },
            new SiLAElement
            {
              Identifier = "QuantityType",
              DataType = new DataTypeType { Item = BasicType.String }
            },
            new SiLAElement
            {
              Identifier = "Unit",
              DataType = new DataTypeType { Item = BasicType.String }
            }
          ]
        }
      })
    {
      Value = new DynamicObject
      {
        Elements =
        {
          new DynamicObjectProperty("Scalar", "Scalar", null, new DataTypeType { Item = BasicType.Real }) { Value = 12.5 },
          new DynamicObjectProperty("QuantityType", "QuantityType", null, new DataTypeType { Item = BasicType.String }) { Value = "Duration" },
          new DynamicObjectProperty("Unit", "Unit", null, new DataTypeType { Item = BasicType.String }) { Value = "s" }
        }
      }
    };

    var value = SilaDataConverter.ToAresValue(property);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(value.KindCase, Is.EqualTo(AresValue.KindOneofCase.StructValue));
      Assert.That(value.StructValue.Fields["Scalar"].NumberValue, Is.EqualTo(12.5));
      Assert.That(value.StructValue.Fields["QuantityType"].StringValue, Is.EqualTo("Duration"));
      Assert.That(value.StructValue.Fields["Unit"].StringValue, Is.EqualTo("s"));
    }
  }

  [Test]
  public void StructValue_RoundTrips_ThroughSilaConverter()
  {
    var input = new AresStruct
    {
      Fields =
      {
        ["Enabled"] = AresValueHelper.CreateBool(true),
        ["Handler"] = AresValueHelper.CreateFunction("fn://demo"),
        ["Nested"] = AresValueHelper.CreateStruct(new AresStruct
        {
          Fields =
          {
            ["Name"] = AresValueHelper.CreateString("Pump"),
            ["Amount"] = AresValueHelper.CreateQuantity(2.5, QuantityType.Duration, "s")
          }
        })
      }
    };

    var silaObject = SilaDataConverter.ToSilaObject(input);
    var output = SilaDataConverter.ToAresStruct(silaObject);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(output.Fields["Enabled"].BoolValue, Is.True);
      Assert.That(output.Fields["Handler"].FunctionValue.FunctionId, Is.EqualTo("fn://demo"));
      Assert.That(output.Fields["Nested"].StructValue.Fields["Name"].StringValue, Is.EqualTo("Pump"));
      Assert.That(output.Fields["Nested"].StructValue.Fields["Amount"].QuantityValue.Scalar, Is.EqualTo(2.5));
      Assert.That(output.Fields["Nested"].StructValue.Fields["Amount"].QuantityValue.Type, Is.EqualTo(QuantityType.Duration));
      Assert.That(output.Fields["Nested"].StructValue.Fields["Amount"].QuantityValue.Unit, Is.EqualTo("s"));
    }
  }

  [Test]
  public void ToAresValueSchema_SilaConstrainedNumberList_PreservesBounds()
  {
    var silaType = new DataTypeType
    {
      Item = new ListType
      {
        DataType = new DataTypeType
        {
          Item = new ConstrainedType
          {
            DataType = new DataTypeType { Item = BasicType.Real },
            Constraints = new Constraints
            {
              MinimalInclusive = "0",
              MaximalInclusive = "10"
            }
          }
        }
      }
    };

    var schema = SilaDataConverter.ToAresValueSchema(silaType);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(schema.Type, Is.EqualTo(AresDataType.NumberArray));
      Assert.That(schema.HasMinNumberValue, Is.True);
      Assert.That(schema.MinNumberValue, Is.EqualTo(0));
      Assert.That(schema.HasMaxNumberValue, Is.True);
      Assert.That(schema.MaxNumberValue, Is.EqualTo(10));
    }
  }

  [Test]
  public void ToAresValueSchema_RegularSilaStructure_BecomesStructSchema()
  {
    var silaType = new DataTypeType
    {
      Item = new StructureType
      {
        Element =
        [
          new SiLAElement
          {
            Identifier = "Name",
            Description = "Device name",
            DataType = new DataTypeType { Item = BasicType.String }
          },
          new SiLAElement
          {
            Identifier = "Enabled",
            DataType = new DataTypeType { Item = BasicType.Boolean }
          }
        ]
      }
    };

    var schema = SilaDataConverter.ToAresValueSchema(silaType);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(schema.Type, Is.EqualTo(AresDataType.Struct));
      Assert.That(schema.StructSchema, Is.Not.Null);
      Assert.That(schema.StructSchema.Fields["Name"].Type, Is.EqualTo(AresDataType.String));
      Assert.That(schema.StructSchema.Fields["Name"].Description, Is.EqualTo("Device name"));
      Assert.That(schema.StructSchema.Fields["Enabled"].Type, Is.EqualTo(AresDataType.Boolean));
    }
  }

  [Test]
  public void ToAresValueSchema_EncodedFunctionStructure_BecomesFunctionSchema()
  {
    var schema = SilaDataConverter.ToAresValueSchema(
      new DataTypeType
      {
        Item = new StructureType
        {
          Element =
          [
            new SiLAElement
            {
              Identifier = "__AresType",
              DataType = new DataTypeType
              {
                Item = new ConstrainedType
                {
                  DataType = new DataTypeType { Item = BasicType.String },
                  Constraints = new Constraints { Set = ["Function"] }
                }
              }
            },
            new SiLAElement
            {
              Identifier = "FunctionId",
              DataType = new DataTypeType { Item = BasicType.String }
            }
          ]
        }
      });

    Assert.That(schema.Type, Is.EqualTo(AresDataType.Function));
  }

  [Test]
  public void ToAresValueSchema_EncodedQuantityStructure_BecomesQuantitySchema()
  {
    var schema = SilaDataConverter.ToAresValueSchema(
      new DataTypeType
      {
        Item = new StructureType
        {
          Element =
          [
            new SiLAElement
            {
              Identifier = "__AresType",
              DataType = new DataTypeType
              {
                Item = new ConstrainedType
                {
                  DataType = new DataTypeType { Item = BasicType.String },
                  Constraints = new Constraints { Set = ["Quantity"] }
                }
              }
            },
            new SiLAElement
            {
              Identifier = "Scalar",
              DataType = new DataTypeType { Item = BasicType.Real }
            },
            new SiLAElement
            {
              Identifier = "QuantityType",
              DataType = new DataTypeType
              {
                Item = new ConstrainedType
                {
                  DataType = new DataTypeType { Item = BasicType.String },
                  Constraints = new Constraints { Set = ["Duration"] }
                }
              }
            },
            new SiLAElement
            {
              Identifier = "Unit",
              DataType = new DataTypeType { Item = BasicType.String }
            }
          ]
        }
      });

    using(Assert.EnterMultipleScope())
    {
      Assert.That(schema.Type, Is.EqualTo(AresDataType.Quantity));
      Assert.That(schema.QuantitySchema, Is.Not.Null);
      Assert.That(schema.QuantitySchema.QuantityType, Is.EqualTo(QuantityType.Duration));
    }
  }

  [Test]
  public void ToAresValue_BinaryPropertyWithStream_ReadsBytes()
  {
    var property = new DynamicObjectProperty(
      "Payload",
      "Payload",
      null,
      new DataTypeType { Item = BasicType.Binary })
    {
      Value = new MemoryStream([1, 2, 3, 4])
    };

    var value = SilaDataConverter.ToAresValue(property);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(value.KindCase, Is.EqualTo(AresValue.KindOneofCase.BytesValue));
      Assert.That(value.BytesValue, Is.EqualTo(ByteString.CopyFrom([1, 2, 3, 4])));
    }
  }
}
