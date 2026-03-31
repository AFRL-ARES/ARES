using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Symbols;
using System.Text;
using UnitsNet;
using UnitsNet.Units;

namespace AresScript;

public static class StandardLibrary
{
  public static AresSystemFunctionSymbol[] Functions { get; } =
  [
    new("print", "print", (args, _) =>
      {
        foreach (var arg in args)
        {
          Console.WriteLine(arg.Stringify());
        }

        return Task.FromResult(AresValueHelper.CreateUnit());
      },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
    Namespace: "")
    {
      Detail = "Prints the given value/s of any ARES type to console.",
      Documentation = "Prints the given value/s of any ARES type to console."
    },

    new("string", "string", (args, token) => {
      var strBuilder = new StringBuilder();
      foreach(var aresValue in args)
      {
        strBuilder.Append(aresValue.Stringify());
      }
      return Task.FromResult(AresValueHelper.CreateString(strBuilder.ToString()));
    },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.String).Build(),
    Namespace: "")
    {
      Detail = "Turns the given AresValue into a string.",
      Documentation = "Turns the given AresValue into a string."
    },

    new("len", "len", (args, _) =>
    {
      if(args.Count != 1)
      {
        throw new InvalidOperationException($"Len expects exactly 1 argument, got {args.Count}.");
      }

      var arg = args[0];
      var length = arg.KindCase switch
      {
        AresValue.KindOneofCase.StringValue => arg.StringValue.Length,
        AresValue.KindOneofCase.StringArrayValue => arg.StringArrayValue.Strings.Count,
        AresValue.KindOneofCase.NumberArrayValue => arg.NumberArrayValue.Numbers.Count,
        AresValue.KindOneofCase.ListValue => arg.ListValue.Values.Count,
        AresValue.KindOneofCase.BytesValue => arg.BytesValue.Length,
        AresValue.KindOneofCase.StructValue => arg.StructValue.Fields.Count,
        _ => throw new InvalidOperationException($"Len is not supported for value type {arg.KindCase}.")
      };

      return Task.FromResult(AresValueHelper.CreateNumber(length));
    },
    AresSchemaBuilder.Create("value", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.Number).Build(),
    "")
    {
      Documentation = "Returns the length of a string, array, list, bytes, or struct."
    },
    
    new("range", "range", (args, _) => {
      double start = 0;
      double stop = 0;
      double step = 1;

      if (args.Count == 1)
      {
        if (!args[0].HasNumberValue)
          throw new InvalidOperationException("Range argument must be a number.");
        stop = args[0].NumberValue;
      }
      else if (args.Count == 2)
      {
        if (!args[0].HasNumberValue || !args[1].HasNumberValue)
          throw new InvalidOperationException("Range arguments must be numbers.");
        start = args[0].NumberValue;
        stop = args[1].NumberValue;
      }
      else if (args.Count == 3)
      {
        if (!args[0].HasNumberValue || !args[1].HasNumberValue || !args[2].HasNumberValue)
          throw new InvalidOperationException("Range arguments must be numbers.");
        start = args[0].NumberValue;
        stop = args[1].NumberValue;
        step = args[2].NumberValue;
      }
      else
      {
        throw new InvalidOperationException($"Range expects 1, 2, or 3 arguments, got {args.Count}.");
      }

      if (Math.Abs(step) < double.Epsilon)
        throw new InvalidOperationException("Range step must not be zero.");

      var count = Math.Max(0, (int)Math.Ceiling((stop - start) / step));
      var numbers = Enumerable.Range(0, count)
                              .Select(i => start + (i * step))
                              .ToArray();

      return Task.FromResult(AresValueHelper.CreateNumberArray(numbers));
    },
    AresSchemaBuilder.Empty()
      .AddEntry("start", AresSchemaBuilder.NumberEntry().AsOptional().WithDescription("The starting number").Build())
      .AddEntry("stop", AresSchemaBuilder.NumberEntry().WithDescription("The non-inclusive stopping number.").Build())
      .AddEntry("step", AresSchemaBuilder.NumberEntry().AsOptional().WithDescription("The step size.").Build())
      .Build(),
    AresSchemaBuilder.Entry(AresDataType.NumberArray).Build(),
    Namespace: "")
    {
      Detail = "Generates a list of numbers in a range.",
      Documentation = "Generates a list of numbers in a range."
    },

    new("sleep", "sleep", async (args, token) => {
      if(args.Count != 1)
      {
        throw new ArgumentException("Expected exactly 1 argument for duration.", nameof(args));
      }

      var remaining = TimeSpan.FromMilliseconds(ToDurationMilliseconds(args[0]));
      var interval = TimeSpan.FromMilliseconds(50);
      while(remaining > TimeSpan.Zero)
      {
        token.ThrowIfCancellationRequested();
        await token.WaitForResumeAsync();

        var delay = remaining < interval ? remaining : interval;
        await Task.Delay(delay, token.CancellationToken);
        remaining -= delay;
      }

      return AresValueHelper.CreateUnit();
    },
    AresSchemaBuilder.Create("time", AresDataType.Any)
      .WithDescription("How much time to sleep. Accepts a Duration quantity or a plain number (treated as milliseconds).")
      .Build(),
    AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
    Namespace: "")
    {
      Detail = "Sleep for a given time",
      Documentation = "Sleep for a time. Accepts a Duration quantity (e.g. Quantity.Duration.from(500, \"ms\")) or a plain number treated as milliseconds (e.g. sleep(500)).",
      StaticArgumentValidator = args =>
      {
        if(args.Count != 1 || args[0] is null)
        {
          return new StaticArgValidation(true);
        }

        var argument = args[0]!;
        if(argument.HasNumberValue)
        {
          return new StaticArgValidation(true);
        }

        if(argument.KindCase == AresValue.KindOneofCase.QuantityValue
          && argument.QuantityValue.TryToUnitsNetQuantity(out var quantity)
          && quantity is not null
          && string.Equals(quantity.QuantityInfo.Name, nameof(UnitsNet.Duration), StringComparison.OrdinalIgnoreCase))
        {
          return new StaticArgValidation(true);
        }

        return new StaticArgValidation(false, "Sleep expects a Duration quantity or a plain number.", 0);
      }
    }
  ];

  public static AresExtensionFunction[] ExtensionFunctions { get; } =
  [
    new(
      AresValue.KindOneofCase.QuantityValue,
      "as",
      new AresSystemFunctionSymbol(
        "quantity::as",
        "as",
        (args, _) =>
        {
          if(args.Count != 2)
          {
            throw new ArgumentException("Expected exactly 1 argument to as.", nameof(args));
          }

          var quantityValue = args[0];
          if(quantityValue.KindCase != AresValue.KindOneofCase.QuantityValue
            || !quantityValue.QuantityValue.TryToUnitsNetQuantity(out var quantity)
            || quantity is null)
          {
            throw new InvalidOperationException("as can only be called on quantity values.");
          }

          if(args[1] is not { HasStringValue: true } unitArg || string.IsNullOrWhiteSpace(unitArg.StringValue))
          {
            throw new InvalidOperationException("as requires a non-empty unit string.");
          }

          if(!TryResolveQuantityType(quantity, out var quantityType, out var quantityTypeError))
          {
            throw new InvalidOperationException(quantityTypeError);
          }

          if(!QuantityUnitHelper.TryParseUnit(quantityType, unitArg.StringValue, out var unit, out var error))
          {
            throw new InvalidOperationException(error);
          }

          return Task.FromResult(AresValueHelper.CreateQuantity(Quantity.From(quantity.As(unit), unit).ToQuantityValue()));
        },
        BuildQuantityAsSchema(),
        AresSchemaBuilder.Entry(AresDataType.Quantity).Build(),
        Namespace: "",
        IsExtension: true
      )
      {
        Detail = "Convert a quantity to a different compatible unit.",
        Documentation = "Converts a quantity to another compatible unit string, for example duration.as(\"ms\").",
        StaticArgumentValidator = args =>
        {
          if(args.Count != 2 || args[0] is null || args[1] is not { HasStringValue: true } unitArg || string.IsNullOrWhiteSpace(unitArg.StringValue))
          {
            return new StaticArgValidation(true);
          }

          var receiver = args[0]!;
          if(receiver.KindCase != AresValue.KindOneofCase.QuantityValue
            || !receiver.QuantityValue.TryToUnitsNetQuantity(out var quantity)
            || quantity is null)
          {
            return new StaticArgValidation(false, "as can only be called on quantity values.", 0);
          }

          if(!TryResolveQuantityType(quantity, out var quantityType, out var quantityTypeError))
          {
            return new StaticArgValidation(false, quantityTypeError, 0);
          }

          if(!QuantityUnitHelper.TryParseUnit(quantityType, unitArg.StringValue, out _, out var error))
          {
            return new StaticArgValidation(false, error, 1);
          }

          return new StaticArgValidation(true);
        }
      }),
    new(
      AresValue.KindOneofCase.ListValue,
      "append",
      new AresSystemFunctionSymbol(
        "list::append",
        "append",
        (args, _) =>
        {
          if(args.Count != 2)
          {
            throw new ArgumentException("Expected exactly 1 argument to append.", nameof(args));
          }

          var listValue = args[0];
          if(listValue.KindCase != AresValue.KindOneofCase.ListValue || listValue.ListValue is null)
          {
            throw new InvalidOperationException("append can only be called on list values.");
          }

          listValue.ListValue.Values.Add(args[1]);
          return Task.FromResult(AresValueHelper.CreateUnit());
        },
        BuildListAppendSchema(),
        AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
        Namespace: "",
        IsExtension: true
      )
      {
        Detail = "Appends a value to the list.",
        Documentation = "Appends a value to the list."
      }),
    new(
      AresValue.KindOneofCase.NumberArrayValue,
      "append",
      new AresSystemFunctionSymbol(
        "number_array::append",
        "append",
        (args, _) =>
        {
          if(args.Count != 2)
          {
            throw new ArgumentException("Expected exactly 1 argument to append.", nameof(args));
          }

          var numberArrayValue = args[0];
          if(numberArrayValue.KindCase != AresValue.KindOneofCase.NumberArrayValue || numberArrayValue.NumberArrayValue is null)
          {
            throw new InvalidOperationException("append can only be called on number array values.");
          }

          if(!args[1].HasNumberValue)
          {
            throw new InvalidOperationException("append argument must be a number for number arrays.");
          }

          numberArrayValue.NumberArrayValue.Numbers.Add(args[1].NumberValue);
          return Task.FromResult(AresValueHelper.CreateUnit());
        },
        BuildNumberArrayAppendSchema(),
        AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
        Namespace: "",
        IsExtension: true
      )
      {
        Detail = "Appends a number to the number array.",
        Documentation = "Appends a number to the number array."
      }),
    new(
      AresValue.KindOneofCase.StringArrayValue,
      "append",
      new AresSystemFunctionSymbol(
        "string_array::append",
        "append",
        (args, _) =>
        {
          if(args.Count != 2)
          {
            throw new ArgumentException("Expected exactly 1 argument to append.", nameof(args));
          }

          var stringArrayValue = args[0];
          if(stringArrayValue.KindCase != AresValue.KindOneofCase.StringArrayValue || stringArrayValue.StringArrayValue is null)
          {
            throw new InvalidOperationException("append can only be called on string array values.");
          }

          if(!args[1].HasStringValue)
          {
            throw new InvalidOperationException("append argument must be a string for string arrays.");
          }

          stringArrayValue.StringArrayValue.Strings.Add(args[1].StringValue);
          return Task.FromResult(AresValueHelper.CreateUnit());
        },
        BuildStringArrayAppendSchema(),
        AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
        Namespace: "",
        IsExtension: true
      )
      {
        Detail = "Appends a string to the string array.",
        Documentation = "Appends a string to the string array."
      })
  ];

  private static AresStructSchema BuildListAppendSchema()
  {
    var listEntry = AresSchemaBuilder.Entry(AresDataType.List).Build();
    listEntry.ListElementSchema = AresSchemaBuilder.Entry(AresDataType.Any).Build();

    return AresSchemaBuilder.Empty()
      .AddEntry("self", listEntry)
      .AddEntry("value", AresSchemaBuilder.Entry(AresDataType.Any).Build())
      .Build();
  }

  private static AresStructSchema BuildQuantityAsSchema()
  {
    return AresSchemaBuilder.Empty()
      .AddEntry("self", AresSchemaBuilder.Entry(AresDataType.Quantity).Build())
      .AddEntry("unit", AresSchemaBuilder.Entry(AresDataType.String).Build())
      .Build();
  }

  private static AresStructSchema BuildNumberArrayAppendSchema()
  {
    return AresSchemaBuilder.Empty()
      .AddEntry("self", AresSchemaBuilder.Entry(AresDataType.NumberArray).Build())
      .AddEntry("value", AresSchemaBuilder.Entry(AresDataType.Number).Build())
      .Build();
  }

  private static AresStructSchema BuildStringArrayAppendSchema()
  {
    return AresSchemaBuilder.Empty()
      .AddEntry("self", AresSchemaBuilder.Entry(AresDataType.StringArray).Build())
      .AddEntry("value", AresSchemaBuilder.Entry(AresDataType.String).Build())
      .Build();
  }

  private static double ToDurationMilliseconds(AresValue value)
  {
    if(value.HasNumberValue)
    {
      return value.NumberValue;
    }

    if(value.KindCase != AresValue.KindOneofCase.QuantityValue)
    {
      throw new InvalidOperationException("Argument provided is not a number or duration quantity.");
    }

    var quantity = value.QuantityValue.ToUnitsNetQuantity();
    if(!string.Equals(quantity.QuantityInfo.Name, "Duration", StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException(
        $"Duration quantity expected but got {quantity.QuantityInfo.Name}.");
    }

    return quantity.As(DurationUnit.Millisecond);
  }

  private static bool TryResolveQuantityType(IQuantity quantity, out QuantityType quantityType, out string? error)
  {
    error = null;
    if(Enum.TryParse<QuantityType>(quantity.QuantityInfo.Name, true, out quantityType)
      && quantityType != QuantityType.Unspecified)
    {
      return true;
    }

    quantityType = QuantityType.Unspecified;
    error = $"Unsupported quantity type '{quantity.QuantityInfo.Name}'.";
    return false;
  }
}
