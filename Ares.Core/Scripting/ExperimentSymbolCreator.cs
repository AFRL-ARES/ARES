using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript;
using AresScript.Symbols;

namespace Ares.Core.Scripting;

public static class ExperimentSymbolCreator
{
  public static AresSystemFunctionSymbol CreateFail()
  {
    var fail = new AresSystemFunctionSymbol(
      "fail",
      "fail",
      (args, _) =>
      {
        if(args.Count != 1)
        {
          throw new InvalidOperationException($"Fail expects exactly 1 argument, got {args.Count}.");
        }

        if(args[0] is not { HasStringValue: true } messageArg || string.IsNullOrWhiteSpace(messageArg.StringValue))
        {
          throw new InvalidOperationException("Fail requires a non-empty message string.");
        }

        throw new InvalidOperationException(messageArg.StringValue);
      },
      AresSchemaBuilder.Create("message", AresDataType.String).Build(),
      AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      "")
    {
      Detail = "Fail the experiment execution with a message.",
      Documentation = "Stops script execution immediately and reports the provided failure message."
    };

    return fail;
  }

  public static AresSystemFunctionSymbol CreatePause(ScriptExecutionControlTokenSource tokenSource)
  {
    var pause = new AresSystemFunctionSymbol(
      "pause",
      "pause",
      (_, __) =>
      {
        tokenSource.Pause();
        return Task.FromResult(AresValueHelper.CreateUnit());
      },
      new AresStructSchema(),
      AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      ""
      )
    {
      Detail = "Pause the experiment execution."
    };

    return pause;
  }

  public static AresSystemFunctionSymbol CreateStop(ScriptExecutionControlTokenSource tokenSource)
  {
    var stop = new AresSystemFunctionSymbol(
      "stop",
      "stop",
      (_, __) =>
      {
        tokenSource.Cancel();
        return Task.FromResult(AresValueHelper.CreateUnit());
      },
      new AresStructSchema(),
      AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      ""
      )
    {
      Detail = "Stop the experiment execution."
    };

    return stop;
  }
}
