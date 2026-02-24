using System.Text;
using Ares.Datamodel.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Factories;

namespace AresScript;

public static class StandardLibrary
{
  public static AresSystemFunction[] Functions { get; } =
  [
    new("print", "print", (args, _) =>
      {
        foreach (var arg in args)
        {
          Console.WriteLine(arg.Value.Stringify());
        }

        return Task.FromResult(AresValueHelper.CreateUnit());
      },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
    "",
    "Prints the given value/s of any ARES type to console."),
    
    new("string", "string", (args, _) => {
      var strBuilder = new StringBuilder();
      foreach(var aresValue in args)
      {
        strBuilder.Append(aresValue.Value.Stringify());
      }
      return Task.FromResult(AresValueHelper.CreateString(strBuilder.ToString()));
    },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.String).Build(),
    "",
    "Turns the given AresValue into a string."),
    
    new("range", "range", (args, _) => {
      double start = 0;
      double stop = 0;
      double step = 1;

      if (args.Count == 1)
      {
        if (!args.First().Value.HasNumberValue)
          throw new InvalidOperationException("Range argument must be a number.");
        stop = args.First().Value.NumberValue;
      }
      else if (args.Count == 2)
      {
        if (!args.First().Value.HasNumberValue || !args.Skip(1).First().Value.HasNumberValue)
          throw new InvalidOperationException("Range arguments must be numbers.");
        start = args.First().Value.NumberValue;
        stop = args.Skip(1).First().Value.NumberValue;
      }
      else if (args.Count == 3)
      {
        if (!args.First().Value.HasNumberValue || !args.Skip(1).First().Value.HasNumberValue || !args.Skip(2).First().Value.HasNumberValue)
          throw new InvalidOperationException("Range arguments must be numbers.");
        start = args.First().Value.NumberValue;
        stop = args.Skip(1).First().Value.NumberValue;
        step = args.Skip(2).First().Value.NumberValue;
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
    "",
    "Generates a list of numbers in a range."),
    
    new("sleep", "sleep", async (args, token) => {
      if(args.Count != 1)
      {
        throw new ArgumentException("Expected exactly 1 argument for duration.", nameof(args));
      }

      if(!args.First().Value.HasNumberValue)
      {
        throw new InvalidOperationException("Argument provided is not a number");
      }

      var remaining = TimeSpan.FromMilliseconds(args.First().Value.NumberValue);
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
    AresSchemaBuilder.Create("time", AresDataType.Number)
      .WithDescription("Number of milliseconds to sleep")
      .WithUnit("ms")
      .Build(),
    AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
    "",
    "Sleep for a given number of milliseconds"
    )
  ];

  public static AresExtensionFunction[] ExtensionFunctions { get; } =
  [
    new(
      AresValue.KindOneofCase.ListValue,
      "append",
      new AresSystemFunction(
        "list::append",
        "append",
        (args, _) =>
        {
          if(args.Count != 2)
          {
            throw new ArgumentException("Expected exactly 1 argument to append.", nameof(args));
          }

          var listValue = args["self"];
          if(listValue.KindCase != AresValue.KindOneofCase.ListValue || listValue.ListValue is null)
          {
            throw new InvalidOperationException("append can only be called on list values.");
          }

          listValue.ListValue.Values.Add(args["0"]);
          return Task.FromResult(AresValueHelper.CreateUnit());
        },
        BuildListAppendSchema(),
        AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
        "",
        "Appends a value to the list."
      ))
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
}