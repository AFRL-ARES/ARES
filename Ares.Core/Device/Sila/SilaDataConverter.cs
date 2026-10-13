using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Google.Protobuf;
using System.Collections;
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

  /// <summary>
  /// Converts an ARES data type into its closest SiLA data type representation.
  /// </summary>
  /// <param name="dataType">The ARES data type to convert.</param>
  /// <returns>A SiLA data type describing the supplied ARES type.</returns>
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
      AresDataType.Any => CreateAnyStructureType(),
      AresDataType.Timestamp => CreateBasicType(BasicType.Timestamp),
      AresDataType.Float => CreateBasicType(BasicType.Real),
      AresDataType.Int => CreateBasicType(BasicType.Integer),
      AresDataType.IntArray => CreateListType(CreateBasicType(BasicType.Integer)),
      AresDataType.FloatArray => CreateListType(CreateBasicType(BasicType.Real)),
      AresDataType.Unit => CreateUnitStructureType(),
      AresDataType.Function => CreateFunctionStructureType(),
      AresDataType.Quantity => CreateQuantityStructureType(),
      _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
  }

  /// <summary>
  /// Converts an ARES schema into a SiLA data type, including nested structures,
  /// list element schemas, and supported constraints.
  /// </summary>
  /// <param name="schema">The ARES schema to convert.</param>
  /// <returns>A SiLA data type describing the supplied schema.</returns>
  public static DataTypeType ToSilaDataType(AresValueSchema schema)
  {
    ArgumentNullException.ThrowIfNull(schema);

    if(schema.Type == AresDataType.Struct)
      return CreateStructureType(schema.StructSchema?.Fields.Select(kvp => CreateElement(kvp.Key, kvp.Value)) ?? []);

    if(schema.Type == AresDataType.List)
    {
      var elementSchema = schema.ListElementSchema ?? AresSchemaBuilder.Entry(AresDataType.Any).Build();
      return CreateListType(elementSchema.Type == AresDataType.Any
        ? CreateBasicType(BasicType.Any)
        : ToSilaDataType(elementSchema));
    }

    if(schema.Type == AresDataType.Unit)
      return CreateUnitStructureType();

    if(schema.Type == AresDataType.Function)
      return CreateFunctionStructureType();

    if(schema.Type == AresDataType.Quantity)
      return CreateQuantityStructureType(schema.QuantitySchema);

    return CreateConstrainedTypeIfNeeded(schema, ToSilaDataType(schema.Type));
  }

  /// <summary>
  /// Converts a SiLA data type into the corresponding ARES data type.
  /// </summary>
  /// <param name="dataType">The SiLA data type to inspect.</param>
  /// <returns>The top-level ARES data type represented by the SiLA type.</returns>
  public static AresDataType ToAresDataType(DataTypeType dataType)
  {
    return ToAresValueSchema(dataType).Type;
  }

  /// <summary>
  /// Converts a SiLA data type into an ARES schema after resolving named types
  /// through the supplied feature-local type resolver.
  /// </summary>
  /// <param name="dataType">The SiLA data type to convert.</param>
  /// <param name="typeResolver">Resolves named SiLA data type identifiers.</param>
  /// <returns>An ARES schema describing the resolved SiLA type.</returns>
  public static AresValueSchema ToAresValueSchema(DataTypeType dataType, Func<string, DataTypeType?> typeResolver)
  {
    ArgumentNullException.ThrowIfNull(typeResolver);

    return ToAresValueSchema(ResolveDataType(dataType, typeResolver));
  }

  /// <summary>
  /// Resolves named SiLA data type references recursively.
  /// </summary>
  /// <param name="dataType">The SiLA data type, which may contain named references.</param>
  /// <param name="typeResolver">Resolves a named type identifier to its definition.</param>
  /// <returns>A copy of the type with all resolvable named references expanded.</returns>
  public static DataTypeType ResolveDataType(DataTypeType dataType, Func<string, DataTypeType?> typeResolver)
  {
    ArgumentNullException.ThrowIfNull(dataType);
    ArgumentNullException.ThrowIfNull(typeResolver);

    return ResolveDataType(dataType, typeResolver, []);
  }

  /// <summary>
  /// Converts a resolved SiLA data type into an ARES schema.
  /// </summary>
  /// <param name="dataType">The resolved SiLA data type to convert.</param>
  /// <returns>An ARES schema describing the supplied type.</returns>
  public static AresValueSchema ToAresValueSchema(DataTypeType dataType)
  {
    ArgumentNullException.ThrowIfNull(dataType);

    return dataType.Item switch
    {
      BasicType.String => AresSchemaBuilder.Entry(AresDataType.String).Build(),
      BasicType.Integer => AresSchemaBuilder.Entry(AresDataType.Int).Build(),
      BasicType.Real => AresSchemaBuilder.Entry(AresDataType.Float).Build(),
      BasicType.Boolean => AresSchemaBuilder.Entry(AresDataType.Boolean).Build(),
      BasicType.Binary => AresSchemaBuilder.Entry(AresDataType.ByteArray).Build(),
      BasicType.Any => AresSchemaBuilder.Entry(AresDataType.Any).Build(),
      BasicType.Date => AresSchemaBuilder.Entry(AresDataType.Timestamp).WithDescription("Converted from SiLA Date").Build(),
      BasicType.Time => AresSchemaBuilder.Entry(AresDataType.Timestamp).WithDescription("Converted from SiLA Time").Build(),
      BasicType.Timestamp => AresSchemaBuilder.Entry(AresDataType.Timestamp).WithDescription("Converted from SiLA Timestamp").Build(),
      ListType listType => CreateAresListSchema(listType),
      StructureType structureType => CreateAresStructSchema(structureType),
      ConstrainedType constrainedType => ApplySilaConstraints(ToAresValueSchema(constrainedType.DataType), constrainedType.Constraints),
      string => AresSchemaBuilder.Entry(AresDataType.Any).Build(),
      _ => AresSchemaBuilder.Entry(AresDataType.Any).Build()
    };
  }

  /// <summary>
  /// Resolves nested named types while preserving the original SiLA type shape.
  /// </summary>
  /// <param name="dataType">The type to resolve.</param>
  /// <param name="typeResolver">Resolves named type identifiers.</param>
  /// <param name="resolvingTypes">The named types currently being resolved.</param>
  /// <returns>A resolved copy of <paramref name="dataType"/>.</returns>
  private static DataTypeType ResolveDataType(DataTypeType dataType, Func<string, DataTypeType?> typeResolver, HashSet<string> resolvingTypes)
  {
    return dataType.Item switch
    {
      BasicType => dataType,
      StructureType structureType => new DataTypeType
      {
        Item = new StructureType
        {
          Element = structureType.Element?.Select(element => new SiLAElement
          {
            Identifier = element.Identifier,
            DisplayName = element.DisplayName,
            Description = element.Description,
            DataType = element.DataType is null
              ? null
              : ResolveDataType(element.DataType, typeResolver, resolvingTypes)
          }).ToArray()
        }
      },
      ListType listType => new DataTypeType
      {
        Item = new ListType
        {
          DataType = listType.DataType is null
            ? null
            : ResolveDataType(listType.DataType, typeResolver, resolvingTypes)
        }
      },
      ConstrainedType constrainedType => new DataTypeType
      {
        Item = new ConstrainedType
        {
          DataType = constrainedType.DataType is null
            ? null
            : ResolveDataType(constrainedType.DataType, typeResolver, resolvingTypes),
          Constraints = constrainedType.Constraints
        }
      },
      string typeName => ResolveNamedType(typeName, typeResolver, resolvingTypes),
      null => dataType,
      _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType.Item, "Unsupported SiLA data type.")
    };
  }

  /// <summary>
  /// Resolves one named SiLA type and detects cyclic references.
  /// </summary>
  /// <param name="typeName">The named type identifier.</param>
  /// <param name="typeResolver">Resolves the identifier to a type definition.</param>
  /// <param name="resolvingTypes">The named types currently being resolved.</param>
  /// <returns>The fully resolved type definition.</returns>
  private static DataTypeType ResolveNamedType(string typeName, Func<string, DataTypeType?> typeResolver, HashSet<string> resolvingTypes)
  {
    if(!resolvingTypes.Add(typeName))
      throw new InvalidOperationException($"Cyclic SiLA data type reference detected for '{typeName}'.");

    try
    {
      var resolvedType = typeResolver(typeName)
        ?? throw new InvalidOperationException($"SiLA data type '{typeName}' was not defined by the feature.");

      return ResolveDataType(resolvedType, typeResolver, resolvingTypes);
    }
    finally
    {
      resolvingTypes.Remove(typeName);
    }
  }

  /// <summary>
  /// Converts an ARES structure into an untyped SiLA dynamic object.
  /// </summary>
  /// <param name="value">The ARES structure to convert.</param>
  /// <returns>A SiLA dynamic object containing the structure fields.</returns>
  public static DynamicObject ToSilaObject(AresStruct value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var result = new DynamicObject();
    foreach(var field in value.Fields)
      result.Elements.Add(ToSilaProperty(field.Key, field.Value));

    return result;
  }

  /// <summary>
  /// Converts an untyped SiLA dynamic object into an ARES structure.
  /// </summary>
  /// <param name="value">The SiLA dynamic object to convert.</param>
  /// <returns>An ARES structure containing the converted fields.</returns>
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

  /// <summary>
  /// Converts a SiLA dynamic object into an ARES structure using a type resolver
  /// for nested named data types.
  /// </summary>
  /// <param name="value">The SiLA dynamic object to convert.</param>
  /// <param name="typeResolver">Resolves named SiLA data type identifiers.</param>
  /// <returns>An ARES structure containing the converted fields.</returns>
  public static AresStruct ToAresStruct(DynamicObject value, Func<string, DataTypeType?> typeResolver)
  {
    ArgumentNullException.ThrowIfNull(value);
    ArgumentNullException.ThrowIfNull(typeResolver);

    var result = new AresStruct();
    foreach(var element in value.Elements)
      result.Fields[element.Identifier] = ToAresValue(element, typeResolver);

    return result;
  }

  /// <summary>
  /// Converts an ARES value into a SiLA dynamic property using the value's
  /// runtime ARES type as the property type.
  /// </summary>
  /// <param name="identifier">The property identifier.</param>
  /// <param name="value">The ARES value to convert.</param>
  /// <param name="description">An optional property description.</param>
  /// <returns>A SiLA dynamic property containing the converted value.</returns>
  public static DynamicObjectProperty ToSilaProperty(string identifier, AresValue value, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(identifier);
    ArgumentNullException.ThrowIfNull(value);

    return new DynamicObjectProperty(identifier, identifier, description, ToSilaDataType(value.ToAresValueSchema()))
    {
      Value = ToSilaRuntimeValue(identifier, value)
    };
  }

  /// <summary>
  /// Converts an ARES value into a SiLA dynamic property using the declared
  /// SiLA type rather than inferring the wire type from the runtime value.
  /// </summary>
  /// <param name="identifier">The property identifier.</param>
  /// <param name="value">The ARES value to convert.</param>
  /// <param name="declaredType">The declared SiLA property type.</param>
  /// <param name="typeResolver">Resolves named SiLA data type identifiers.</param>
  /// <param name="description">An optional property description.</param>
  /// <returns>A SiLA dynamic property containing the converted value.</returns>
  public static DynamicObjectProperty ToSilaProperty(string identifier, AresValue value, DataTypeType declaredType, Func<string, DataTypeType?> typeResolver, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(identifier);
    ArgumentNullException.ThrowIfNull(value);
    ArgumentNullException.ThrowIfNull(declaredType);
    ArgumentNullException.ThrowIfNull(typeResolver);

    var resolvedType = ResolveDataType(declaredType, typeResolver);
    return new DynamicObjectProperty(identifier, identifier, description, declaredType)
    {
      Value = ToSilaRuntimeValue(identifier, value, resolvedType)
    };
  }

  /// <summary>
  /// Converts a SiLA dynamic property into an ARES value using its declared type.
  /// </summary>
  /// <param name="property">The SiLA property to convert.</param>
  /// <returns>An ARES value representing the property payload.</returns>
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
      BasicType.Integer => AresValueHelper.CreateInt(Convert.ToInt64(property.Value ?? 0L, CultureInfo.InvariantCulture)),
      BasicType.Real => AresValueHelper.CreateFloat(Convert.ToDouble(property.Value ?? 0d, CultureInfo.InvariantCulture)),
      BasicType.Boolean => AresValueHelper.CreateBool(property.Value is bool value && value),
      BasicType.Binary => AresValueHelper.CreateBytes(ToByteArray(property.Value)),
      BasicType.Date => AresValueHelper.CreateTimestamp(ToDateTime(property.Value)),
      BasicType.Time => AresValueHelper.CreateTimestamp(ToDateTime(property.Value)),
      BasicType.Timestamp => AresValueHelper.CreateTimestamp(ToDateTime(property.Value)),
      BasicType.Any => ToAresAnyValue(property.Value),
      ListType listType => ToAresListValue(listType, property.Value),
      StructureType structureType => IsAnyStructureType(structureType)
        ? ToAresAnyValue(property.Value)
        : ToAresStructureValue(property.Value),
      string => ToAresAnyValue(property.Value),
      _ => AresValueHelper.CreateNull()
    };
  }

  /// <summary>
  /// Converts a SiLA dynamic property into an ARES value after resolving named
  /// types in the property's declared type.
  /// </summary>
  /// <param name="property">The SiLA property to convert.</param>
  /// <param name="typeResolver">Resolves named SiLA data type identifiers.</param>
  /// <returns>An ARES value representing the property payload.</returns>
  public static AresValue ToAresValue(DynamicObjectProperty property, Func<string, DataTypeType?> typeResolver)
  {
    ArgumentNullException.ThrowIfNull(property);
    ArgumentNullException.ThrowIfNull(typeResolver);

    return ToAresValue(property, ResolveDataType(property.Type, typeResolver), typeResolver);
  }

  /// <summary>
  /// Converts a property using an already-resolved SiLA type.
  /// </summary>
  /// <param name="property">The SiLA property to convert.</param>
  /// <param name="resolvedType">The resolved type of the property.</param>
  /// <param name="typeResolver">An optional resolver for nested named types.</param>
  /// <returns>An ARES value representing the property payload.</returns>
  private static AresValue ToAresValue(DynamicObjectProperty property, DataTypeType resolvedType, Func<string, DataTypeType?>? typeResolver = null)
  {
    return resolvedType.Item switch
    {
      ConstrainedType constrainedType => ToAresValue(
        new DynamicObjectProperty(property.Identifier, property.DisplayName, property.Description, constrainedType.DataType)
        {
          Value = property.Value
        },
        constrainedType.DataType,
        typeResolver),
      BasicType.String => AresValueHelper.CreateString(property.Value as string ?? string.Empty),
      BasicType.Integer => AresValueHelper.CreateInt(Convert.ToInt64(property.Value ?? 0L, CultureInfo.InvariantCulture)),
      BasicType.Real => AresValueHelper.CreateFloat(Convert.ToDouble(property.Value ?? 0d, CultureInfo.InvariantCulture)),
      BasicType.Boolean => AresValueHelper.CreateBool(property.Value is bool value && value),
      BasicType.Binary => AresValueHelper.CreateBytes(ToByteArray(property.Value)),
      BasicType.Date => AresValueHelper.CreateTimestamp(ToDateTime(property.Value)),
      BasicType.Time => AresValueHelper.CreateTimestamp(ToDateTime(property.Value)),
      BasicType.Timestamp => AresValueHelper.CreateTimestamp(ToDateTime(property.Value)),
      BasicType.Any => ToAresAnyValue(property.Value, typeResolver),
      ListType listType => ToAresListValue(listType, property.Value, typeResolver),
      StructureType structureType => IsAnyStructureType(structureType)
        ? ToAresAnyValue(property.Value, typeResolver)
        : ToAresStructureValue(property.Value, typeResolver),
      string => ToAresAnyValue(property.Value, typeResolver),
      null => AresValueHelper.CreateNull(),
      _ => throw new ArgumentOutOfRangeException(nameof(resolvedType), resolvedType.Item, "Unsupported SiLA data type.")
    };
  }

  /// <summary>
  /// Converts an ARES value into an untyped SiLA runtime payload.
  /// </summary>
  /// <param name="identifier">The identifier used when reporting conversion errors.</param>
  /// <param name="value">The ARES value to convert.</param>
  /// <returns>The runtime object expected by the SiLA dynamic client.</returns>
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
      AresValue.KindOneofCase.TimestampValue => value.TimestampValue.ToDateTimeOffset(),
      AresValue.KindOneofCase.FloatValue => value.FloatValue,
      AresValue.KindOneofCase.IntValue => value.IntValue,
      AresValue.KindOneofCase.IntArrayValue => value.IntArrayValue.Ints.Cast<object>().ToList(),
      AresValue.KindOneofCase.FloatArrayValue => value.FloatArrayValue.Floats.Cast<object>().ToList(),
      AresValue.KindOneofCase.AnyValue => ToSilaAnyValue(value.AnyValue),
      _ => throw new ArgumentOutOfRangeException(nameof(value.KindCase), value.KindCase, $"Unsupported ARES value kind for '{identifier}'.")
    };
  }

  /// <summary>
  /// Converts an ARES value into a SiLA runtime payload according to a declared type.
  /// </summary>
  /// <param name="identifier">The identifier used when reporting conversion errors.</param>
  /// <param name="value">The ARES value to convert.</param>
  /// <param name="declaredType">The declared SiLA type to enforce.</param>
  /// <returns>The runtime object expected by the SiLA dynamic client.</returns>
  private static object? ToSilaRuntimeValue(string identifier, AresValue value, DataTypeType declaredType)
  {
    return declaredType.Item switch
    {
      BasicType basicType => ToSilaBasicRuntimeValue(identifier, value, basicType),
      ListType listType => ToSilaListRuntimeValue(identifier, value, listType.DataType),
      StructureType structureType => ToSilaStructureRuntimeValue(identifier, value, structureType),
      ConstrainedType constrainedType => ToSilaRuntimeValue(identifier, value, constrainedType.DataType),
      string => ToSilaRuntimeValue(identifier, value),
      null => ToSilaRuntimeValue(identifier, value),
      _ => throw new ArgumentOutOfRangeException(nameof(declaredType), declaredType.Item, "Unsupported SiLA data type.")
    };
  }

  /// <summary>
  /// Converts an ARES value into a runtime value for a SiLA basic type.
  /// </summary>
  /// <param name="identifier">The identifier used when reporting conversion errors.</param>
  /// <param name="value">The ARES value to convert.</param>
  /// <param name="basicType">The SiLA basic type to enforce.</param>
  /// <returns>The converted basic runtime value.</returns>
  private static object? ToSilaBasicRuntimeValue(string identifier, AresValue value, BasicType basicType)
  {
    var unwrappedValue = value.KindCase == AresValue.KindOneofCase.AnyValue ? value.AnyValue : value;

    return basicType switch
    {
      BasicType.String => unwrappedValue.KindCase == AresValue.KindOneofCase.StringValue 
        ? unwrappedValue.StringValue
        : throw new InvalidOperationException($"Expected a string value for SiLA element '{identifier}'."),

      BasicType.Integer => unwrappedValue.KindCase switch
      {
        AresValue.KindOneofCase.IntValue => unwrappedValue.IntValue,
        AresValue.KindOneofCase.NumberValue => Convert.ToInt64(unwrappedValue.NumberValue, CultureInfo.InvariantCulture),
        AresValue.KindOneofCase.FloatValue => Convert.ToInt64(unwrappedValue.FloatValue, CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"Expected an integer value for SiLA element '{identifier}'.")
      },
      BasicType.Real => unwrappedValue.KindCase switch
      {
        AresValue.KindOneofCase.NumberValue => unwrappedValue.NumberValue,
        AresValue.KindOneofCase.FloatValue => unwrappedValue.FloatValue,
        AresValue.KindOneofCase.IntValue => Convert.ToDouble(unwrappedValue.IntValue, CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"Expected a real value for SiLA element '{identifier}'.")
      },
      BasicType.Boolean => unwrappedValue.KindCase == AresValue.KindOneofCase.BoolValue
        ? unwrappedValue.BoolValue
        : throw new InvalidOperationException($"Expected a boolean value for SiLA element '{identifier}'."),
      BasicType.Binary => unwrappedValue.KindCase == AresValue.KindOneofCase.BytesValue
        ? unwrappedValue.BytesValue.ToByteArray()
        : throw new InvalidOperationException($"Expected a binary value for SiLA element '{identifier}'."),
      BasicType.Date or BasicType.Time or BasicType.Timestamp => ToDateTime(unwrappedValue),
      BasicType.Any => ToSilaAnyProperty(identifier, unwrappedValue),
      _ => throw new ArgumentOutOfRangeException(nameof(basicType), basicType, "Unsupported SiLA basic type.")
    };
  }

  /// <summary>
  /// Converts an ARES list or array value into a SiLA list with the declared
  /// element type applied to every item.
  /// </summary>
  /// <param name="identifier">The identifier used when reporting conversion errors.</param>
  /// <param name="value">The ARES list or array value to convert.</param>
  /// <param name="elementType">The declared SiLA element type.</param>
  /// <returns>A list of SiLA runtime values.</returns>
  private static List<object> ToSilaListRuntimeValue(string identifier, AresValue value, DataTypeType elementType)
  {
    var elements = GetAresListElements(value);
    return elements
      .Select(element => ToSilaRuntimeValue(identifier, element, elementType))
      .Where(element => element is not null)
      .Cast<object>()
      .ToList();
  }

  /// <summary>
  /// Converts an ARES structure into a SiLA dynamic object according to the
  /// declared structure elements.
  /// </summary>
  /// <param name="identifier">The identifier used when reporting conversion errors.</param>
  /// <param name="value">The ARES structure value to convert.</param>
  /// <param name="structureType">The declared SiLA structure type.</param>
  /// <returns>A SiLA dynamic object containing the declared structure fields.</returns>
  private static DynamicObject ToSilaStructureRuntimeValue(string identifier, AresValue value, StructureType structureType)
  {
    var unwrappedValue = value.KindCase == AresValue.KindOneofCase.AnyValue ? value.AnyValue : value;

    if(unwrappedValue.KindCase != AresValue.KindOneofCase.StructValue)
      throw new InvalidOperationException($"Expected a structure value for SiLA element '{identifier}'.");

    var result = new DynamicObject();
    foreach(var element in structureType.Element ?? [])
    {
      if(!unwrappedValue.StructValue.Fields.TryGetValue(element.Identifier, out var fieldValue) || element.DataType is null)
        continue;

      result.Elements.Add(new DynamicObjectProperty(element)
      {
        Value = ToSilaRuntimeValue(element.Identifier, fieldValue, element.DataType)
      });
    }

    return result;
  }

  /// <summary>
  /// Wraps an ARES value in the dynamic property shape used by SiLA Any.
  /// </summary>
  /// <param name="identifier">The property identifier.</param>
  /// <param name="value">The ARES value to wrap.</param>
  /// <returns>A dynamic property carrying the Any payload.</returns>
  private static DynamicObjectProperty ToSilaAnyProperty(string identifier, AresValue value)
    => new DynamicObjectProperty(identifier, identifier, null, ToSilaDataType(value.ToAresValueSchema()))
    {
      Value = ToSilaRuntimeValue(identifier, value)
    };

  /// <summary>
  /// Extracts the logical elements from an ARES list or array representation.
  /// </summary>
  /// <param name="value">The ARES list or array value to inspect.</param>
  /// <returns>The individual ARES elements represented by the value.</returns>
  private static IEnumerable<AresValue> GetAresListElements(AresValue value)
  {
    var unwrappedValue = value.KindCase == AresValue.KindOneofCase.AnyValue ? value.AnyValue : value;

    return unwrappedValue.KindCase switch
    {
      AresValue.KindOneofCase.ListValue => unwrappedValue.ListValue.Values,
      AresValue.KindOneofCase.StringArrayValue => unwrappedValue.StringArrayValue.Strings.Select(AresValueHelper.CreateString),
      AresValue.KindOneofCase.NumberArrayValue => unwrappedValue.NumberArrayValue.Numbers.Select(AresValueHelper.CreateNumber),
      AresValue.KindOneofCase.IntArrayValue => unwrappedValue.IntArrayValue.Ints.Select(AresValueHelper.CreateInt),
      AresValue.KindOneofCase.FloatArrayValue => unwrappedValue.FloatArrayValue.Floats.Select(AresValueHelper.CreateFloat),
      _ => throw new InvalidOperationException("Expected a list value for a SiLA list type.")
    };
  }

  /// <summary>
  /// Wraps an ARES value in the structured dynamic-object representation used
  /// for SiLA Any values.
  /// </summary>
  /// <param name="value">The ARES value to wrap.</param>
  /// <returns>A SiLA dynamic object with a <c>Value</c> property.</returns>
  private static DynamicObject ToSilaAnyValue(AresValue value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var dynamicObject = new DynamicObject();
    dynamicObject.Elements.Add(ToSilaProperty(_anyValueIdentifier, value));
    return dynamicObject;
  }

  /// <summary>
  /// Converts ARES list values into untyped SiLA list payloads, inferring a
  /// common element type from the supplied values.
  /// </summary>
  /// <param name="values">The ARES values to convert.</param>
  /// <returns>A list of SiLA runtime values.</returns>
  private static List<object> ToSilaListValue(IEnumerable<AresValue> values)
  {
    var list = values.ToList();
    if(list.Count == 0)
      return [];

    var inferredSchema = AresValueHelper.CreateList(list).ToAresValueSchema();
    var itemSchema = inferredSchema.ListElementSchema ?? AresSchemaBuilder.Entry(AresDataType.Any).Build();
    var itemType = itemSchema.Type == AresDataType.Any ? CreateBasicType(BasicType.Any) : ToSilaDataType(itemSchema);
    var converted = new List<object>();

    foreach(var item in list)
      converted.Add(itemType.Item is BasicType.Any ? ToSilaProperty(_anyValueIdentifier, item) : ToSilaRuntimeValue(_anyValueIdentifier, item)!);

    return converted;
  }

  /// <summary>
  /// Converts an untyped SiLA Any payload into an ARES Any-wrapped value.
  /// </summary>
  /// <param name="value">The runtime payload received from SiLA.</param>
  /// <returns>An ARES value whose <c>AnyValue</c> contains the converted payload.</returns>
  private static AresValue ToAresAnyValue(object? value)
  {
    if(value is null)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateNull());

    if(value is AnyTypeDto anyType)
      return ToAresAnyValue(anyType.Extract(null));

    if(value is DynamicObjectProperty property)
    {
      var converted = ToAresValue(property);
      return converted.KindCase == AresValue.KindOneofCase.AnyValue
        ? converted
        : AresValueHelper.CreateAnyAresValue(converted);
    }

    if(value is DynamicObject dynamicObject)
    {
      if(IsAnyObject(dynamicObject))
      {
        var innerProperty = dynamicObject.Elements.Single(element => element.Identifier == _anyValueIdentifier);
        return ToAresAnyValue(innerProperty);
      }

      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateStruct(ToAresStruct(dynamicObject)));
    }

    if(value is string stringValue)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateString(stringValue));

    if(value is bool boolValue)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateBool(boolValue));

    if(IsNumeric(value))
      return AresValueHelper.CreateAnyAresValue(value is byte or sbyte or short or ushort or int or uint or long or ulong
          ? AresValueHelper.CreateInt(Convert.ToInt64(value, CultureInfo.InvariantCulture))
          : AresValueHelper.CreateFloat(Convert.ToDouble(value, CultureInfo.InvariantCulture)));

    if(value is byte[] bytes)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateBytes(bytes));

    if(value is DateTimeOffset dateTimeOffset)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateTimestamp(dateTimeOffset.UtcDateTime));

    if(value is DateTime dateTime)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateTimestamp(dateTime));

    if(value is IEnumerable enumerable)
      return AresValueHelper.CreateAnyAresValue(
        ToAresListValue((ListType)CreateListType(CreateBasicType(BasicType.Any)).Item, enumerable.Cast<object>().ToList()));

    throw new InvalidOperationException($"Unsupported SiLA Any payload type '{value.GetType().FullName}'.");
  }

  /// <summary>
  /// Converts an untyped SiLA Any payload into an ARES Any-wrapped value while
  /// using a resolver for nested named types when necessary.
  /// </summary>
  /// <param name="value">The runtime payload received from SiLA.</param>
  /// <param name="typeResolver">An optional resolver for nested named types.</param>
  /// <returns>An ARES value whose <c>AnyValue</c> contains the converted payload.</returns>
  private static AresValue ToAresAnyValue(object? value, Func<string, DataTypeType?>? typeResolver)
  {
    if(typeResolver is null)
      return ToAresAnyValue(value);

    if(value is null)
      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateNull());

    if(value is AnyTypeDto anyType)
      return ToAresAnyValue(anyType.Extract(null), typeResolver);

    if(value is DynamicObjectProperty property)
    {
      var converted = ToAresValue(property, typeResolver);
      return converted.KindCase == AresValue.KindOneofCase.AnyValue
        ? converted
        : AresValueHelper.CreateAnyAresValue(converted);
    }

    if(value is DynamicObject dynamicObject)
    {
      if(IsAnyObject(dynamicObject))
      {
        var innerProperty = dynamicObject.Elements.Single(element => element.Identifier == _anyValueIdentifier);
        return ToAresAnyValue(innerProperty, typeResolver);
      }

      return AresValueHelper.CreateAnyAresValue(AresValueHelper.CreateStruct(ToAresStruct(dynamicObject, typeResolver)));
    }

    return ToAresAnyValue(value);
  }

  /// <summary>
  /// Converts an untyped SiLA list payload into the corresponding ARES list or array value.
  /// </summary>
  /// <param name="listType">The declared SiLA list type.</param>
  /// <param name="value">The runtime list payload received from SiLA.</param>
  /// <returns>An ARES list or compact array value.</returns>
  private static AresValue ToAresListValue(ListType listType, object? value)
  {
    var values = value switch
    {
      IEnumerable<object> objectValues => objectValues,
      IEnumerable enumerable => enumerable.Cast<object>(),
      _ => []
    };
    var items = values.ToList();

    if(listType.DataType.Item is BasicType.String && items.All(item => item is string))
      return AresValueHelper.CreateStringArray(items.Cast<string>());

    if(IsNumericType(listType.DataType) && items.All(item => item is not null && IsNumeric(item)))
      return AresValueHelper.CreateNumberArray(items.Select(item => Convert.ToDouble(item, CultureInfo.InvariantCulture)));

    var converted = items.Select(item => listType.DataType.Item is BasicType.Any
      ? ToAresAnyValue(item)
      : ToAresValue(new DynamicObjectProperty(_anyValueIdentifier, _anyValueIdentifier, null, listType.DataType) { Value = item }))
      .ToList();

    return AresValueHelper.CreateList(converted);
  }

  /// <summary>
  /// Converts a SiLA list payload into an ARES list or array value while resolving
  /// named element types as needed.
  /// </summary>
  /// <param name="listType">The declared SiLA list type.</param>
  /// <param name="value">The runtime list payload received from SiLA.</param>
  /// <param name="typeResolver">An optional resolver for named element types.</param>
  /// <returns>An ARES list or compact array value.</returns>
  private static AresValue ToAresListValue(ListType listType, object? value, Func<string, DataTypeType?>? typeResolver)
  {
    var values = value switch
    {
      IEnumerable<object> objectValues => objectValues,
      IEnumerable enumerable => enumerable.Cast<object>(),
      _ => []
    };
    var items = values.ToList();

    if(listType.DataType?.Item is BasicType.String && items.All(item => item is string))
      return AresValueHelper.CreateStringArray(items.Cast<string>());

    if(listType.DataType is not null && IsNumericType(listType.DataType) && items.All(item => item is not null && IsNumeric(item)))
      return AresValueHelper.CreateNumberArray(items.Select(item => Convert.ToDouble(item, CultureInfo.InvariantCulture)));

    var elementType = listType.DataType ?? CreateBasicType(BasicType.Any);
    var converted = items
      .Select(item => ToAresValue(
        new DynamicObjectProperty(_anyValueIdentifier, _anyValueIdentifier, null, elementType) { Value = item },
        elementType,
        typeResolver))
      .ToList();

    return AresValueHelper.CreateList(converted);
  }

  /// <summary>
  /// Converts an untyped SiLA structure payload into an ARES structure or one
  /// of the encoded ARES structure-backed values.
  /// </summary>
  /// <param name="value">The runtime structure payload received from SiLA.</param>
  /// <returns>An ARES structure-backed value.</returns>
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
      var quantityType = Enum.TryParse<QuantityType>(GetStringValue(dynamicObject, _quantityTypeIdentifier), true, out var parsed) ? parsed : QuantityType.Unspecified;
      return AresValueHelper.CreateQuantity(GetDoubleValue(dynamicObject, _quantityScalarIdentifier), quantityType, GetStringValue(dynamicObject, _quantityUnitIdentifier));
    }

    return AresValueHelper.CreateStruct(ToAresStruct(dynamicObject));
  }

  /// <summary>
  /// Converts a SiLA structure payload into an ARES structure or encoded value,
  /// using a resolver for nested named types.
  /// </summary>
  /// <param name="value">The runtime structure payload received from SiLA.</param>
  /// <param name="typeResolver">An optional resolver for nested named types.</param>
  /// <returns>An ARES structure-backed value.</returns>
  private static AresValue ToAresStructureValue(object? value, Func<string, DataTypeType?>? typeResolver)
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

    return AresValueHelper.CreateStruct(typeResolver is null ? ToAresStruct(dynamicObject) : ToAresStruct(dynamicObject, typeResolver));
  }

  /// <summary>
  /// Creates an ARES schema for a SiLA list, using compact ARES array types for
  /// lists of strings and numeric values where possible.
  /// </summary>
  /// <param name="listType">The SiLA list type to convert.</param>
  /// <returns>An ARES schema representing the list.</returns>
  private static AresValueSchema CreateAresListSchema(ListType listType)
  {
    var elementSchema = ToAresValueSchema(listType.DataType);

    if(elementSchema.Type == AresDataType.String)
      return CreateArraySchema(AresDataType.StringArray, elementSchema);

    if(elementSchema.Type is AresDataType.Number or AresDataType.Float)
      return CreateArraySchema(AresDataType.NumberArray, elementSchema);

    return AresSchemaBuilder.Entry(AresDataType.List)
      .WithListElementSchema(elementSchema)
      .Build();
  }

  /// <summary>
  /// Creates an ARES schema for a SiLA structure, recognizing ARES-encoded
  /// Unit, Function, Quantity, and Any structures.
  /// </summary>
  /// <param name="structureType">The SiLA structure type to convert.</param>
  /// <returns>An ARES schema representing the structure.</returns>
  private static AresValueSchema CreateAresStructSchema(StructureType structureType)
  {
    if(IsAnyStructureType(structureType))
      return AresSchemaBuilder.Entry(AresDataType.Any).Build();

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

  /// <summary>
  /// Copies supported SiLA constraints onto an ARES schema.
  /// </summary>
  /// <param name="schema">The unconstrained ARES schema.</param>
  /// <param name="constraints">The SiLA constraints to apply.</param>
  /// <returns>An ARES schema containing the supported constraints.</returns>
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

    if(schema.Type is AresDataType.Number or AresDataType.NumberArray or AresDataType.Float or AresDataType.FloatArray)
    {
      if(TryParseDouble(constraints.MinimalInclusive, out var minInclusive))
        constrainedSchema.MinNumberValue = minInclusive;

      if(TryParseDouble(constraints.MaximalInclusive, out var maxInclusive))
        constrainedSchema.MaxNumberValue = maxInclusive;
    }

    return constrainedSchema;
  }

  /// <summary>
  /// Wraps a SiLA base type in a constrained type when the ARES schema contains
  /// representable choices or numeric bounds.
  /// </summary>
  /// <param name="schema">The ARES schema containing constraints.</param>
  /// <param name="baseType">The unconstrained SiLA base type.</param>
  /// <returns>The base type or its constrained SiLA representation.</returns>
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

  /// <summary>
  /// Creates the encoded dynamic structure used to represent an ARES Unit value.
  /// </summary>
  /// <returns>A SiLA dynamic object containing the Unit marker.</returns>
  private static DynamicObject CreateUnitMarkerObject()
  {
    var dynamicObject = new DynamicObject();
    dynamicObject.Elements.Add(CreateEncodedAresTypeProperty(_encodedUnitTypeValue));
    return dynamicObject;
  }

  /// <summary>
  /// Creates the encoded dynamic structure used to represent an ARES function value.
  /// </summary>
  /// <param name="functionId">The function identifier to encode.</param>
  /// <returns>A SiLA dynamic object containing the function marker and identifier.</returns>
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

  /// <summary>
  /// Creates the encoded dynamic structure used to represent an ARES quantity value.
  /// </summary>
  /// <param name="quantity">The quantity to encode.</param>
  /// <returns>A SiLA dynamic object containing the quantity marker and fields.</returns>
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

  /// <summary>
  /// Creates a SiLA structure element from an ARES field schema.
  /// </summary>
  /// <param name="identifier">The field identifier.</param>
  /// <param name="schema">The ARES field schema.</param>
  /// <returns>A SiLA element describing the field.</returns>
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

  /// <summary>
  /// Creates a SiLA data type containing a basic type item.
  /// </summary>
  /// <param name="basicType">The SiLA basic type.</param>
  /// <returns>A SiLA data type wrapping <paramref name="basicType"/>.</returns>
  private static DataTypeType CreateBasicType(BasicType basicType)
    => new DataTypeType { Item = basicType };

  /// <summary>
  /// Creates the structured SiLA representation used for ARES Any values.
  /// </summary>
  /// <returns>A SiLA structure type containing the Any <c>Value</c> element.</returns>
  private static DataTypeType CreateAnyStructureType()
  {
    return CreateStructureType(
    [
      new SiLAElement
      {
        Identifier = _anyValueIdentifier,
        DisplayName = _anyValueIdentifier,
        DataType = CreateBasicType(BasicType.Any)
      }
    ]);
  }

  /// <summary>
  /// Creates a SiLA list type for the supplied element type.
  /// </summary>
  /// <param name="itemType">The list element type.</param>
  /// <returns>A SiLA list data type.</returns>
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

  /// <summary>
  /// Creates a SiLA structure type from its element definitions.
  /// </summary>
  /// <param name="elements">The structure element definitions.</param>
  /// <returns>A SiLA structure data type.</returns>
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

  /// <summary>
  /// Creates the SiLA structure type used to encode ARES Unit values.
  /// </summary>
  /// <returns>A SiLA Unit marker structure type.</returns>
  private static DataTypeType CreateUnitStructureType()
  {
    return CreateStructureType(
    [
      CreateEncodedAresTypeElement(_encodedUnitTypeValue)
    ]);
  }

  /// <summary>
  /// Creates the SiLA structure type used to encode ARES function values.
  /// </summary>
  /// <returns>A SiLA function structure type.</returns>
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

  /// <summary>
  /// Creates the SiLA structure type used to encode ARES quantity values.
  /// </summary>
  /// <param name="quantitySchema">Optional quantity constraints to encode.</param>
  /// <returns>A SiLA quantity structure type.</returns>
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

  /// <summary>
  /// Determines whether a SiLA structure is the encoded ARES Any shape.
  /// </summary>
  /// <param name="structureType">The structure type to inspect.</param>
  /// <returns><see langword="true"/> when the structure contains the Any <c>Value</c> element.</returns>
  private static bool IsAnyStructureType(StructureType structureType)
  {
    return structureType.Element?.Length == 1 &&
           structureType.Element[0].Identifier == _anyValueIdentifier &&
           structureType.Element[0].DataType?.Item is BasicType.Any;
  }

  /// <summary>
  /// Determines whether a dynamic object is the encoded ARES Any wrapper.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <returns><see langword="true"/> when the object contains only the Any <c>Value</c> property.</returns>
  private static bool IsAnyObject(DynamicObject dynamicObject)
  {
    return dynamicObject.Elements.Count == 1 &&
           dynamicObject.Elements[0].Identifier == _anyValueIdentifier;
  }

  /// <summary>
  /// Determines whether a dynamic object has the encoded ARES quantity shape.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <returns><see langword="true"/> when the object represents a quantity.</returns>
  private static bool IsQuantityObject(DynamicObject dynamicObject)
  {
    var elementIds = dynamicObject.Elements.Select(element => element.Identifier).ToHashSet(StringComparer.Ordinal);
    return elementIds.SetEquals([_encodedAresTypeIdentifier, _quantityScalarIdentifier, _quantityTypeIdentifier, _quantityUnitIdentifier]) &&
           HasEncodedAresType(dynamicObject, _encodedQuantityTypeValue);
  }

  /// <summary>
  /// Determines whether a dynamic object has the encoded ARES function shape.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <returns><see langword="true"/> when the object represents a function.</returns>
  private static bool IsFunctionObject(DynamicObject dynamicObject)
  {
    return dynamicObject.Elements.Count == 2 &&
           dynamicObject.Elements.Any(element => element.Identifier == _functionIdentifier) &&
           HasEncodedAresType(dynamicObject, _encodedFunctionTypeValue);
  }

  /// <summary>
  /// Determines whether a dynamic object has the encoded ARES Unit shape.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <returns><see langword="true"/> when the object represents Unit.</returns>
  private static bool IsUnitMarkerObject(DynamicObject dynamicObject)
  {
    return dynamicObject.Elements.Count == 1 &&
           HasEncodedAresType(dynamicObject, _encodedUnitTypeValue);
  }

  /// <summary>
  /// Creates an ARES array schema from a SiLA list element schema and carries
  /// over compatible choices and numeric bounds.
  /// </summary>
  /// <param name="arrayType">The ARES array type to create.</param>
  /// <param name="elementSchema">The schema of one list element.</param>
  /// <returns>An ARES array schema.</returns>
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

  /// <summary>
  /// Checks whether a structure contains the requested encoded ARES type marker.
  /// </summary>
  /// <param name="structureType">The structure type to inspect.</param>
  /// <param name="encodedType">The encoded ARES type name.</param>
  /// <returns><see langword="true"/> when the marker matches.</returns>
  private static bool IsEncodedAresType(StructureType structureType, string encodedType)
  {
    return HasEncodedAresType(structureType, encodedType);
  }

  /// <summary>
  /// Checks the encoded ARES type marker inside a SiLA structure definition.
  /// </summary>
  /// <param name="structureType">The structure type to inspect.</param>
  /// <param name="encodedType">The encoded ARES type name.</param>
  /// <returns><see langword="true"/> when the structure contains the marker.</returns>
  private static bool HasEncodedAresType(StructureType structureType, string encodedType)
  {
    var markerElement = structureType.Element?.FirstOrDefault(element => element.Identifier == _encodedAresTypeIdentifier);
    return markerElement?.DataType?.Item is ConstrainedType constrained &&
           constrained.DataType.Item is BasicType.String &&
           constrained.Constraints?.Set?.Length == 1 &&
           string.Equals(constrained.Constraints.Set[0], encodedType, StringComparison.Ordinal);
  }

  /// <summary>
  /// Checks the encoded ARES type marker inside a dynamic object.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <param name="encodedType">The encoded ARES type name.</param>
  /// <returns><see langword="true"/> when the object contains the marker.</returns>
  private static bool HasEncodedAresType(DynamicObject dynamicObject, string encodedType)
  {
    return string.Equals(GetStringValue(dynamicObject, _encodedAresTypeIdentifier), encodedType, StringComparison.Ordinal);
  }

  /// <summary>
  /// Creates a dynamic property containing an encoded ARES type marker.
  /// </summary>
  /// <param name="encodedType">The ARES type name to encode.</param>
  /// <returns>A dynamic marker property.</returns>
  private static DynamicObjectProperty CreateEncodedAresTypeProperty(string encodedType)
  {
    return new DynamicObjectProperty(_encodedAresTypeIdentifier, _encodedAresTypeIdentifier, null, CreateEncodedAresTypeDataType(encodedType))
    {
      Value = encodedType
    };
  }

  /// <summary>
  /// Creates a SiLA structure element containing an encoded ARES type marker.
  /// </summary>
  /// <param name="encodedType">The ARES type name to encode.</param>
  /// <returns>A SiLA marker element.</returns>
  private static SiLAElement CreateEncodedAresTypeElement(string encodedType)
  {
    return new SiLAElement
    {
      Identifier = _encodedAresTypeIdentifier,
      DisplayName = _encodedAresTypeIdentifier,
      DataType = CreateEncodedAresTypeDataType(encodedType)
    };
  }

  /// <summary>
  /// Creates the constrained SiLA string type used for an encoded ARES marker.
  /// </summary>
  /// <param name="encodedType">The ARES type name to encode.</param>
  /// <returns>A constrained SiLA string data type.</returns>
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

  /// <summary>
  /// Reads a string property from a dynamic object, returning an empty string when absent.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <param name="identifier">The property identifier to read.</param>
  /// <returns>The property's string value or an empty string.</returns>
  private static string GetStringValue(DynamicObject dynamicObject, string identifier)
    => dynamicObject.Elements.FirstOrDefault(element => element.Identifier == identifier)?.Value as string ?? string.Empty;

  /// <summary>
  /// Reads a numeric property from a dynamic object, returning zero when absent.
  /// </summary>
  /// <param name="dynamicObject">The dynamic object to inspect.</param>
  /// <param name="identifier">The property identifier to read.</param>
  /// <returns>The property's numeric value or zero.</returns>
  private static double GetDoubleValue(DynamicObject dynamicObject, string identifier)
  {
    var value = dynamicObject.Elements.FirstOrDefault(element => element.Identifier == identifier)?.Value;
    return value is null ? 0d : Convert.ToDouble(value, CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Parses an invariant-culture floating-point value used by SiLA constraints.
  /// </summary>
  /// <param name="value">The text to parse.</param>
  /// <param name="result">The parsed value when parsing succeeds.</param>
  /// <returns><see langword="true"/> when the value is a valid floating-point number.</returns>
  private static bool TryParseDouble(string? value, out double result)
    => double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

  /// <summary>
  /// Determines whether a SiLA data type is an integer or real numeric type.
  /// </summary>
  /// <param name="dataType">The SiLA data type to inspect.</param>
  /// <returns><see langword="true"/> when the type is numeric.</returns>
  private static bool IsNumericType(DataTypeType dataType)
    => dataType.Item is BasicType.Integer or BasicType.Real;

  /// <summary>
  /// Determines whether a runtime value is one of the supported numeric CLR types.
  /// </summary>
  /// <param name="value">The runtime value to inspect.</param>
  /// <returns><see langword="true"/> when the value is numeric.</returns>
  private static bool IsNumeric(object value)
    => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

  /// <summary>
  /// Converts supported binary runtime representations into a byte array.
  /// </summary>
  /// <param name="value">The binary runtime value to convert.</param>
  /// <returns>The converted bytes, or an empty array for unsupported or null values.</returns>
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

  /// <summary>
  /// Reads a stream into a byte array while restoring its original position when possible.
  /// </summary>
  /// <param name="stream">The stream to read.</param>
  /// <returns>The stream contents as bytes.</returns>
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

  /// <summary>
  /// Converts supported temporal runtime representations to UTC <see cref="DateTime"/>.
  /// </summary>
  /// <param name="value">The temporal value to convert.</param>
  /// <returns>The converted UTC value, or the Unix epoch when conversion fails.</returns>
  private static DateTime ToDateTime(object? value)
  {
    return value switch
    {
      DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime,
      DateTime dateTime => dateTime.ToUniversalTime(),
      _ => DateTime.TryParse(value?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
        ? parsed.ToUniversalTime()
        : DateTime.UnixEpoch
    };
  }
}
