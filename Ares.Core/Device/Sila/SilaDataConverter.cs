using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Google.Protobuf;
using System.Globalization;
using Tecan.Sila2;
using Tecan.Sila2.DynamicClient;

namespace Ares.Core.Device.Sila;

public static class SilaDataConverter
{
  private const string _encodedAresTypeIdentifier = "__AresType";
  private const string _encodedUnitTypeValue = "Unit";
  private const string _encodedFunctionTypeValue = "Function";
  private const string _encodedQuantityTypeValue = "Quantity";
  private const string _functionIdentifier = "FunctionId";
  private const string _quantityScalarIdentifier = "Scalar";
  private const string _quantityTypeIdentifier = "QuantityType";
  private const string _quantityUnitIdentifier = "Unit";
  private const string _anyValueIdentifier = "Value";

  public static DataTypeType ToSilaDataType(AresDataType dataType)
  {
    return dataType switch
    {
      AresDataType.UnspecifiedType => CreateBasicType(BasicType.Any),
      AresDataType.Null => CreateBasicType(BasicType.Any),
      AresDataType.Boolean => CreateBasicType(BasicType.Boolean),
      AresDataType.String => CreateBasicType(BasicType.String),
      AresDataType.Number => CreateBasicType(BasicType.Real),
      AresDataType.StringArray => CreateListType(CreateBasicType(BasicType.String)),
      AresDataType.NumberArray => CreateListType(CreateBasicType(BasicType.Real)),
      AresDataType.List => CreateListType(CreateBasicType(BasicType.Any)),
      AresDataType.Struct => CreateStructureType([]),
      AresDataType.ByteArray => CreateBasicType(BasicType.Binary),
      AresDataType.Any => CreateBasicType(BasicType.Any),
      AresDataType.Unit => CreateUnitStructureType(),
      AresDataType.Function => CreateFunctionStructureType(),
      AresDataType.Quantity => CreateQuantityStructureType(),
      _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
  }

  public static DataTypeType ToSilaDataType(AresValueSchema schema)
  {
    ArgumentNullException.ThrowIfNull(schema);

    if(schema.Type == AresDataType.Struct)
    {
      return CreateStructureType(
        schema.StructSchema?.Fields.Select(kvp => CreateElement(kvp.Key, kvp.Value)) ?? []);
    }

    if(schema.Type == AresDataType.List)
    {
      return CreateListType(ToSilaDataType(schema.ListElementSchema ?? AresSchemaBuilder.Entry(AresDataType.Any).Build()));
    }

    if(schema.Type == AresDataType.Unit)
      return CreateUnitStructureType();

    if(schema.Type == AresDataType.Function)
      return CreateFunctionStructureType();

    if(schema.Type == AresDataType.Quantity)
      return CreateQuantityStructureType(schema.QuantitySchema);

    return CreateConstrainedTypeIfNeeded(schema, ToSilaDataType(schema.Type));
  }

  public static AresDataType ToAresDataType(DataTypeType dataType)
  {
    return ToAresValueSchema(dataType).Type;
  }

  public static AresValueSchema ToAresValueSchema(DataTypeType dataType)
  {
    ArgumentNullException.ThrowIfNull(dataType);

    return dataType.Item switch
    {
      BasicType.String => AresSchemaBuilder.Entry(AresDataType.String).Build(),
      // TODO fix when we have int/float support in ARES
      BasicType.Integer => AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      BasicType.Real => AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      BasicType.Boolean => AresSchemaBuilder.Entry(AresDataType.Boolean).Build(),
      BasicType.Binary => AresSchemaBuilder.Entry(AresDataType.ByteArray).Build(),
      BasicType.Any => AresSchemaBuilder.Entry(AresDataType.Any).Build(),
      // TODO fix when we have proper date types in ARES
      BasicType.Date => AresSchemaBuilder.Entry(AresDataType.String).WithDescription("Converted from SiLA Date").Build(),
      BasicType.Time => AresSchemaBuilder.Entry(AresDataType.String).WithDescription("Converted from SiLA Time").Build(),
      BasicType.Timestamp => AresSchemaBuilder.Entry(AresDataType.String).WithDescription("Converted from SiLA Timestamp").Build(),
      ListType listType => CreateAresListSchema(listType),
      StructureType structureType => CreateAresStructSchema(structureType),
      ConstrainedType constrainedType => ApplySilaConstraints(ToAresValueSchema(constrainedType.DataType), constrainedType.Constraints),
      string => AresSchemaBuilder.Entry(AresDataType.Any).Build(),
      _ => AresSchemaBuilder.Entry(AresDataType.Any).Build()
    };
  }

  public static DynamicObject ToSilaObject(AresStruct value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var result = new DynamicObject();
    foreach(var field in value.Fields)
    {
      result.Elements.Add(ToSilaProperty(field.Key, field.Value));
    }

    return result;
  }

  public static AresStruct ToAresStruct(DynamicObject value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var result = new AresStruct();
    foreach(var element in value.Elements)
    {
      result.Fields[element.Identifier] = ToAresValue(element);
    }

    return result;
  }

  public static DynamicObjectProperty ToSilaProperty(string identifier, AresValue value, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(identifier);
    ArgumentNullException.ThrowIfNull(value);

    return new DynamicObjectProperty(identifier, identifier, description, ToSilaDataType(value.ToAresValueSchema()))
    {
      Value = ToSilaRuntimeValue(identifier, value)
    };
  }

  public static AresValue ToAresValue(DynamicObjectProperty property)
  {
    ArgumentNullException.ThrowIfNull(property);

    return property.Type.Item switch
    {
      ConstrainedType constrainedType => ToAresValue(new DynamicObjectProperty(property.Identifier, property.DisplayName, property.Description, constrainedType.DataType)
      {
        Value = property.Value
      }),
      BasicType.String => AresValueHelper.CreateString(property.Value as string ?? string.Empty),
      BasicType.Integer => AresValueHelper.CreateNumber(Convert.ToDouble(property.Value ?? 0L, CultureInfo.InvariantCulture)),
      BasicType.Real => AresValueHelper.CreateNumber(Convert.ToDouble(property.Value ?? 0d, CultureInfo.InvariantCulture)),
      BasicType.Boolean => AresValueHelper.CreateBool(property.Value is bool value && value),
      BasicType.Binary => AresValueHelper.CreateBytes(ToByteArray(property.Value)),
      BasicType.Date => AresValueHelper.CreateString(ToIsoString(property.Value, "yyyy-MM-dd")),
      BasicType.Time => AresValueHelper.CreateString(ToIsoString(property.Value, "c")),
      BasicType.Timestamp => AresValueHelper.CreateString(ToIsoString(property.Value, "O")),
      BasicType.Any => ToAresAnyValue(property.Value),
      ListType listType => ToAresListValue(listType, property.Value),
      StructureType _ => ToAresStructureValue(property.Value),
      string => ToAresAnyValue(property.Value),
      _ => AresValueHelper.CreateNull()
    };
  }

  private static object? ToSilaRuntimeValue(string identifier, AresValue value)
  {
    return value.KindCase switch
    {
      AresValue.KindOneofCase.None => null,
      AresValue.KindOneofCase.NullValue => null,
      AresValue.KindOneofCase.BoolValue => value.BoolValue,
      AresValue.KindOneofCase.StringValue => value.StringValue,
      AresValue.KindOneofCase.NumberValue => value.NumberValue,
      AresValue.KindOneofCase.BytesValue => value.BytesValue.ToByteArray(),
      AresValue.KindOneofCase.StringArrayValue => value.StringArrayValue.Strings.Cast<object>().ToList(),
      AresValue.KindOneofCase.NumberArrayValue => value.NumberArrayValue.Numbers.Cast<object>().ToList(),
      AresValue.KindOneofCase.ListValue => ToSilaListValue(value.ListValue.Values),
      AresValue.KindOneofCase.StructValue => ToSilaObject(value.StructValue),
      AresValue.KindOneofCase.UnitValue => CreateUnitMarkerObject(),
      AresValue.KindOneofCase.FunctionValue => CreateFunctionObject(value.FunctionValue.FunctionId),
      AresValue.KindOneofCase.QuantityValue => CreateQuantityObject(value.QuantityValue),
      _ => throw new ArgumentOutOfRangeException(nameof(value.KindCase), value.KindCase, $"Unsupported ARES value kind for '{identifier}'.")
    };
  }

  private static List<object> ToSilaListValue(IEnumerable<AresValue> values)
  {
    var list = values.ToList();
    if(list.Count == 0)
      return [];

    var inferredSchema = AresValueHelper.CreateList(list).ToAresValueSchema();
    var itemType = ToSilaDataType(inferredSchema.ListElementSchema ?? AresSchemaBuilder.Entry(AresDataType.Any).Build());
    var converted = new List<object>();

    foreach(var item in list)
    {
      converted.Add(itemType.Item is BasicType.Any
        ? ToSilaProperty(_anyValueIdentifier, item)
        : ToSilaRuntimeValue(_anyValueIdentifier, item)!);
    }

    return converted;
  }

  private static AresValue ToAresAnyValue(object? value)
  {
    if(value is null)
      return AresValueHelper.CreateNull();

    if(value is DynamicObjectProperty property)
      return ToAresValue(property);

    if(value is DynamicObject dynamicObject)
      return AresValueHelper.CreateStruct(ToAresStruct(dynamicObject));

    if(value is string stringValue)
      return AresValueHelper.CreateString(stringValue);

    if(value is bool boolValue)
      return AresValueHelper.CreateBool(boolValue);

    if(IsNumeric(value))
      return AresValueHelper.CreateNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));

    if(value is byte[] bytes)
      return AresValueHelper.CreateBytes(bytes);

    if(value is IEnumerable<object> enumerable)
      return ToAresListValue((ListType)CreateListType(CreateBasicType(BasicType.Any)).Item, enumerable.ToList());

    return AresValueHelper.CreateString(value.ToString() ?? string.Empty);
  }

  private static AresValue ToAresListValue(ListType listType, object? value)
  {
    var values = value as IEnumerable<object> ?? [];
    var items = values.ToList();

    if(listType.DataType.Item is BasicType.String && items.All(item => item is string))
      return AresValueHelper.CreateStringArray(items.Cast<string>());

    if(IsNumericType(listType.DataType) && items.All(item => item is not null && IsNumeric(item)))
      return AresValueHelper.CreateNumberArray(items.Select(item => Convert.ToDouble(item, CultureInfo.InvariantCulture)));

    var converted = items.Select(item => listType.DataType.Item is BasicType.Any && item is not DynamicObjectProperty
      ? ToAresAnyValue(item)
      : ToAresValue(new DynamicObjectProperty(_anyValueIdentifier, _anyValueIdentifier, null, listType.DataType) { Value = item }))
      .ToList();

    return AresValueHelper.CreateList(converted);
  }

  private static AresValue ToAresStructureValue(object? value)
  {
    if(value is not DynamicObject dynamicObject)
      return AresValueHelper.CreateStruct();

    if(IsUnitMarkerObject(dynamicObject))
      return AresValueHelper.CreateUnit();

    if(IsFunctionObject(dynamicObject))
      return AresValueHelper.CreateFunction(GetStringValue(dynamicObject, _functionIdentifier));

    if(IsQuantityObject(dynamicObject))
    {
      var quantityType = Enum.TryParse<QuantityType>(GetStringValue(dynamicObject, _quantityTypeIdentifier), true, out var parsed)
        ? parsed
        : QuantityType.Unspecified;

      return AresValueHelper.CreateQuantity(
        GetDoubleValue(dynamicObject, _quantityScalarIdentifier),
        quantityType,
        GetStringValue(dynamicObject, _quantityUnitIdentifier));
    }

    return AresValueHelper.CreateStruct(ToAresStruct(dynamicObject));
  }

  private static AresValueSchema CreateAresListSchema(ListType listType)
  {
    var elementSchema = ToAresValueSchema(listType.DataType);

    if(elementSchema.Type == AresDataType.String)
      return CreateArraySchema(AresDataType.StringArray, elementSchema);

    if(elementSchema.Type == AresDataType.Number)
      return CreateArraySchema(AresDataType.NumberArray, elementSchema);

    return AresSchemaBuilder.Entry(AresDataType.List)
      .WithListElementSchema(elementSchema)
      .Build();
  }

  private static AresValueSchema CreateAresStructSchema(StructureType structureType)
  {
    if(IsEncodedAresType(structureType, _encodedUnitTypeValue))
      return AresSchemaBuilder.Entry(AresDataType.Unit).Build();

    if(IsEncodedAresType(structureType, _encodedFunctionTypeValue))
      return AresSchemaBuilder.Entry(AresDataType.Function).Build();

    if(IsEncodedAresType(structureType, _encodedQuantityTypeValue))
    {
      var schema = AresSchemaBuilder.Entry(AresDataType.Quantity);
      var quantityElement = structureType.Element.FirstOrDefault(element => element.Identifier == _quantityTypeIdentifier);
      if(quantityElement?.DataType?.Item is ConstrainedType constrained &&
         constrained.Constraints?.Set?.Length == 1 &&
         Enum.TryParse<QuantityType>(constrained.Constraints.Set[0], true, out var quantityType))
      {
        schema.WithQuantity(quantityType);
      }

      return schema.Build();
    }

    var structSchema = new AresStructSchema();
    foreach(var element in structureType.Element ?? [])
    {
      var elementSchema = ToAresValueSchema(element.DataType);
      if(!string.IsNullOrWhiteSpace(element.Description))
        elementSchema.Description = element.Description;

      structSchema.Fields[element.Identifier] = elementSchema;
    }

    return AresSchemaBuilder.Entry(AresDataType.Struct)
      .WithStructSchema(structSchema)
      .Build();
  }

  private static AresValueSchema ApplySilaConstraints(AresValueSchema schema, Constraints? constraints)
  {
    if(constraints is null)
      return schema;

    var constrainedSchema = new AresValueSchema
    {
      Type = schema.Type,
      Optional = schema.Optional,
      Description = schema.Description
    };

    if(schema.StructSchema is not null)
      constrainedSchema.StructSchema = schema.StructSchema;

    if(schema.ListElementSchema is not null)
      constrainedSchema.ListElementSchema = schema.ListElementSchema;

    if(schema.QuantitySchema is not null)
      constrainedSchema.QuantitySchema = schema.QuantitySchema;

    if(schema.Type is AresDataType.String or AresDataType.StringArray && constraints.Set?.Length > 0)
    {
      constrainedSchema.StringChoices = new StringArray();
      constrainedSchema.StringChoices.Strings.AddRange(constraints.Set);
    }

    if(schema.Type is AresDataType.Number or AresDataType.NumberArray)
    {
      if(TryParseDouble(constraints.MinimalInclusive, out var minInclusive))
        constrainedSchema.MinNumberValue = minInclusive;

      if(TryParseDouble(constraints.MaximalInclusive, out var maxInclusive))
        constrainedSchema.MaxNumberValue = maxInclusive;
    }

    return constrainedSchema;
  }

  private static DataTypeType CreateConstrainedTypeIfNeeded(AresValueSchema schema, DataTypeType baseType)
  {
    var hasStringChoices = schema.Type == AresDataType.String && schema.StringChoices?.Strings.Count > 0;
    var hasNumberRange = schema.Type == AresDataType.Number && (schema.HasMinNumberValue || schema.HasMaxNumberValue);

    if(!hasStringChoices && !hasNumberRange)
      return baseType;

    var constraints = new Constraints();

    if(hasStringChoices)
      constraints.Set = schema.StringChoices?.Strings.ToArray() ?? [];

    if(schema.Type == AresDataType.Number)
    {
      if(schema.HasMinNumberValue)
        constraints.MinimalInclusive = schema.MinNumberValue.ToString(CultureInfo.InvariantCulture);

      if(schema.HasMaxNumberValue)
        constraints.MaximalInclusive = schema.MaxNumberValue.ToString(CultureInfo.InvariantCulture);
    }

    return new DataTypeType
    {
      Item = new ConstrainedType
      {
        DataType = baseType,
        Constraints = constraints
      }
    };
  }

  private static DynamicObject CreateUnitMarkerObject()
  {
    var dynamicObject = new DynamicObject();
    dynamicObject.Elements.Add(CreateEncodedAresTypeProperty(_encodedUnitTypeValue));
    return dynamicObject;
  }

  private static DynamicObject CreateFunctionObject(string functionId)
  {
    var dynamicObject = new DynamicObject();
    dynamicObject.Elements.Add(CreateEncodedAresTypeProperty(_encodedFunctionTypeValue));
    dynamicObject.Elements.Add(new DynamicObjectProperty(_functionIdentifier, _functionIdentifier, null, CreateBasicType(BasicType.String))
    {
      Value = functionId
    });
    return dynamicObject;
  }

  private static DynamicObject CreateQuantityObject(QuantityValue quantity)
  {
    var dynamicObject = new DynamicObject();
    dynamicObject.Elements.Add(CreateEncodedAresTypeProperty(_encodedQuantityTypeValue));
    dynamicObject.Elements.Add(new DynamicObjectProperty(_quantityScalarIdentifier, _quantityScalarIdentifier, null, CreateBasicType(BasicType.Real))
    {
      Value = quantity.Scalar
    });
    dynamicObject.Elements.Add(new DynamicObjectProperty(_quantityTypeIdentifier, _quantityTypeIdentifier, null, CreateBasicType(BasicType.String))
    {
      Value = quantity.Type.ToString()
    });
    dynamicObject.Elements.Add(new DynamicObjectProperty(_quantityUnitIdentifier, _quantityUnitIdentifier, null, CreateBasicType(BasicType.String))
    {
      Value = quantity.Unit
    });
    return dynamicObject;
  }

  private static SiLAElement CreateElement(string identifier, AresValueSchema schema)
  {
    return new SiLAElement
    {
      Identifier = identifier,
      DisplayName = identifier,
      Description = schema.Description,
      DataType = ToSilaDataType(schema)
    };
  }

  private static DataTypeType CreateBasicType(BasicType basicType)
  {
    return new DataTypeType { Item = basicType };
  }

  private static DataTypeType CreateListType(DataTypeType itemType)
  {
    return new DataTypeType
    {
      Item = new ListType
      {
        DataType = itemType
      }
    };
  }

  private static DataTypeType CreateStructureType(IEnumerable<SiLAElement> elements)
  {
    return new DataTypeType
    {
      Item = new StructureType
      {
        Element = elements.ToArray()
      }
    };
  }

  private static DataTypeType CreateUnitStructureType()
  {
    return CreateStructureType(
    [
      CreateEncodedAresTypeElement(_encodedUnitTypeValue)
    ]);
  }

  private static DataTypeType CreateFunctionStructureType()
  {
    return CreateStructureType(
    [
      CreateEncodedAresTypeElement(_encodedFunctionTypeValue),
      new SiLAElement
      {
        Identifier = _functionIdentifier,
        DisplayName = _functionIdentifier,
        DataType = CreateBasicType(BasicType.String)
      }
    ]);
  }

  private static DataTypeType CreateQuantityStructureType(QuantitySchema? quantitySchema = null)
  {
    DataTypeType quantityType = CreateBasicType(BasicType.String);
    if(quantitySchema is not null && quantitySchema.QuantityType != QuantityType.Unspecified)
    {
      quantityType = new DataTypeType
      {
        Item = new ConstrainedType
        {
          DataType = CreateBasicType(BasicType.String),
          Constraints = new Constraints
          {
            Set = [quantitySchema.QuantityType.ToString()]
          }
        }
      };
    }

    return CreateStructureType(
    [
      CreateEncodedAresTypeElement(_encodedQuantityTypeValue),
      new SiLAElement
      {
        Identifier = _quantityScalarIdentifier,
        DisplayName = _quantityScalarIdentifier,
        DataType = CreateBasicType(BasicType.Real)
      },
      new SiLAElement
      {
        Identifier = _quantityTypeIdentifier,
        DisplayName = _quantityTypeIdentifier,
        DataType = quantityType
      },
      new SiLAElement
      {
        Identifier = _quantityUnitIdentifier,
        DisplayName = _quantityUnitIdentifier,
        DataType = CreateBasicType(BasicType.String)
      }
    ]);
  }

  private static bool IsQuantityType(StructureType structureType)
  {
    var elementIds = structureType.Element?.Select(element => element.Identifier).ToHashSet(StringComparer.Ordinal) ?? [];
    return elementIds.SetEquals([_encodedAresTypeIdentifier, _quantityScalarIdentifier, _quantityTypeIdentifier, _quantityUnitIdentifier]) &&
           HasEncodedAresType(structureType, _encodedQuantityTypeValue);
  }

  private static bool IsFunctionType(StructureType structureType)
  {
    return structureType.Element?.Length == 2 &&
           structureType.Element.Any(element => element.Identifier == _functionIdentifier) &&
           HasEncodedAresType(structureType, _encodedFunctionTypeValue);
  }

  private static bool IsUnitMarkerType(StructureType structureType)
  {
    return structureType.Element?.Length == 1 &&
           HasEncodedAresType(structureType, _encodedUnitTypeValue);
  }

  private static bool IsQuantityObject(DynamicObject dynamicObject)
  {
    var elementIds = dynamicObject.Elements.Select(element => element.Identifier).ToHashSet(StringComparer.Ordinal);
    return elementIds.SetEquals([_encodedAresTypeIdentifier, _quantityScalarIdentifier, _quantityTypeIdentifier, _quantityUnitIdentifier]) &&
           HasEncodedAresType(dynamicObject, _encodedQuantityTypeValue);
  }

  private static bool IsFunctionObject(DynamicObject dynamicObject)
  {
    return dynamicObject.Elements.Count == 2 &&
           dynamicObject.Elements.Any(element => element.Identifier == _functionIdentifier) &&
           HasEncodedAresType(dynamicObject, _encodedFunctionTypeValue);
  }

  private static bool IsUnitMarkerObject(DynamicObject dynamicObject)
  {
    return dynamicObject.Elements.Count == 1 &&
           HasEncodedAresType(dynamicObject, _encodedUnitTypeValue);
  }

  private static AresValueSchema CreateArraySchema(AresDataType arrayType, AresValueSchema elementSchema)
  {
    var schema = new AresValueSchema
    {
      Type = arrayType,
      Optional = elementSchema.Optional,
      Description = elementSchema.Description
    };

    if(arrayType == AresDataType.StringArray && elementSchema.AvailableChoicesCase == AresValueSchema.AvailableChoicesOneofCase.StringChoices)
    {
      schema.StringChoices = new StringArray();
      schema.StringChoices.Strings.AddRange(elementSchema.StringChoices.Strings);
    }

    if(arrayType == AresDataType.NumberArray)
    {
      if(elementSchema.AvailableChoicesCase == AresValueSchema.AvailableChoicesOneofCase.NumberChoices)
      {
        schema.NumberChoices = new NumberArray();
        schema.NumberChoices.Numbers.AddRange(elementSchema.NumberChoices.Numbers);
      }

      if(elementSchema.HasMinNumberValue)
        schema.MinNumberValue = elementSchema.MinNumberValue;

      if(elementSchema.HasMaxNumberValue)
        schema.MaxNumberValue = elementSchema.MaxNumberValue;
    }

    return schema;
  }

  private static bool IsEncodedAresType(StructureType structureType, string encodedType)
  {
    return HasEncodedAresType(structureType, encodedType);
  }

  private static bool HasEncodedAresType(StructureType structureType, string encodedType)
  {
    var markerElement = structureType.Element?.FirstOrDefault(element => element.Identifier == _encodedAresTypeIdentifier);
    return markerElement?.DataType?.Item is ConstrainedType constrained &&
           constrained.DataType.Item is BasicType.String &&
           constrained.Constraints?.Set?.Length == 1 &&
           string.Equals(constrained.Constraints.Set[0], encodedType, StringComparison.Ordinal);
  }

  private static bool HasEncodedAresType(DynamicObject dynamicObject, string encodedType)
  {
    return string.Equals(GetStringValue(dynamicObject, _encodedAresTypeIdentifier), encodedType, StringComparison.Ordinal);
  }

  private static DynamicObjectProperty CreateEncodedAresTypeProperty(string encodedType)
  {
    return new DynamicObjectProperty(_encodedAresTypeIdentifier, _encodedAresTypeIdentifier, null, CreateEncodedAresTypeDataType(encodedType))
    {
      Value = encodedType
    };
  }

  private static SiLAElement CreateEncodedAresTypeElement(string encodedType)
  {
    return new SiLAElement
    {
      Identifier = _encodedAresTypeIdentifier,
      DisplayName = _encodedAresTypeIdentifier,
      DataType = CreateEncodedAresTypeDataType(encodedType)
    };
  }

  private static DataTypeType CreateEncodedAresTypeDataType(string encodedType)
  {
    return new DataTypeType
    {
      Item = new ConstrainedType
      {
        DataType = CreateBasicType(BasicType.String),
        Constraints = new Constraints
        {
          Set = [encodedType]
        }
      }
    };
  }

  private static string GetStringValue(DynamicObject dynamicObject, string identifier)
  {
    return dynamicObject.Elements.FirstOrDefault(element => element.Identifier == identifier)?.Value as string ?? string.Empty;
  }

  private static double GetDoubleValue(DynamicObject dynamicObject, string identifier)
  {
    var value = dynamicObject.Elements.FirstOrDefault(element => element.Identifier == identifier)?.Value;
    return value is null ? 0d : Convert.ToDouble(value, CultureInfo.InvariantCulture);
  }

  private static bool TryParseDouble(string? value, out double result)
  {
    return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
  }

  private static bool IsNumericType(DataTypeType dataType)
  {
    return dataType.Item is BasicType.Integer or BasicType.Real;
  }

  private static bool IsNumeric(object value)
  {
    return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
  }

  private static byte[] ToByteArray(object? value)
  {
    return value switch
    {
      null => [],
      byte[] bytes => bytes,
      ByteString byteString => byteString.ToByteArray(),
      Stream stream => ReadStream(stream),
      _ => []
    };
  }

  private static byte[] ReadStream(Stream stream)
  {
    if(stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer))
      return buffer.Array?[buffer.Offset..(buffer.Offset + (int)memoryStream.Length)] ?? memoryStream.ToArray();

    var originalPosition = stream.CanSeek ? stream.Position : 0;
    if(stream.CanSeek)
      stream.Position = 0;

    using var copy = new MemoryStream();
    stream.CopyTo(copy);

    if(stream.CanSeek)
      stream.Position = originalPosition;

    return copy.ToArray();
  }

  private static string ToIsoString(object? value, string format)
  {
    return value switch
    {
      null => string.Empty,
      DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(format, CultureInfo.InvariantCulture),
      DateTime dateTime => dateTime.ToString(format, CultureInfo.InvariantCulture),
      TimeSpan timeSpan => timeSpan.ToString(format, CultureInfo.InvariantCulture),
      _ => value.ToString() ?? string.Empty
    };
  }
}
