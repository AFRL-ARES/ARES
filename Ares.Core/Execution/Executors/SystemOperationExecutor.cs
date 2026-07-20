using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using UnitsNet;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace Ares.Core.Execution.Executors;

public static class SystemOperationExecutor
{
  public static async Task<CommandResult> Execute(
    SystemOperation operation,
    IReadOnlyList<Parameter> argumentBindings,
    CancellationToken token)
  {
    var argument = argumentBindings.FirstOrDefault()?.GetValue();

    return operation switch
    {
      SystemOperation.SleepForMilliseconds => await Sleep(argument, value => Duration.FromMilliseconds(value), token),
      SystemOperation.SleepForSeconds => await Sleep(argument, value => Duration.FromSeconds(value), token),
      SystemOperation.SleepForMinutes => await Sleep(argument, value => Duration.FromMinutes(value), token),
      SystemOperation.WaitForUser or SystemOperation.WaitForUserInput => new CommandResult
      {
        Success = true,
        AwaitUserInput = true
      },
      SystemOperation.GetTimestamp => new CommandResult
      {
        Success = true,
        Result = AresValueHelper.CreateTimestamp(Timestamp.FromDateTime(DateTime.UtcNow))
      },
      SystemOperation.CalculateAverage => CalculateAverage(argument),
      _ => new CommandResult
      {
        Success = false,
        Error = $"Unsupported system operation: {operation}"
      }
    };
  }

  private static async Task<CommandResult> Sleep(
    AresValue? durationValue,
    Func<double, Duration> createDuration,
    CancellationToken token)
  {
    if(durationValue?.HasNumberValue != true)
    {
      return new CommandResult
      {
        Success = false,
        Error = "Cannot use a sleep operation without specifying a numeric duration."
      };
    }

    await Task.Delay(createDuration(durationValue.NumberValue).ToTimeSpan(), token);
    return new CommandResult { Success = true };
  }

  private static CommandResult CalculateAverage(AresValue? value)
  {
    if(value is null)
    {
      return new CommandResult
      {
        Success = false,
        Error = "ARES was asked to average a list of data, but no argument was provided."
      };
    }

    return value.KindCase switch
    {
      AresValue.KindOneofCase.NumberArrayValue => SuccessfulAverage(value.NumberArrayValue.Numbers.Average()),
      AresValue.KindOneofCase.FloatArrayValue => SuccessfulAverage(value.FloatArrayValue.Floats.Average()),
      AresValue.KindOneofCase.IntArrayValue => SuccessfulAverage(value.IntArrayValue.Ints.Average()),
      _ => new CommandResult
      {
        Success = false,
        Error = $"ARES was asked to average a list of data, but an invalid argument was provided of type {value.KindCase}."
      }
    };
  }

  private static CommandResult SuccessfulAverage(double average)
  {
    return new CommandResult
    {
      Success = true,
      Result = AresValueHelper.CreateFloat(average)
    };
  }
}
