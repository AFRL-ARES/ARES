using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

internal static class ExecutorSummaryHelpers
{
  public static ExperimentExecutionSummary CreateExperimentExecutionSummary(ExperimentOverview experimentOverview,
    DateTime startTime,
    DateTime endTime)
  {
    var experimentSummary = new ExperimentExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      ExperimentId = experimentOverview.Template.UniqueId,
      ExperimentOverview = experimentOverview,
    };

    return experimentSummary;
  }

  private static ExecutionInfo MakeExecutionInfo(DateTime startTime, DateTime endTime)
    => new()
    {
      UniqueId = Guid.NewGuid().ToString(),
      TimeFinished = endTime.ToTimestamp(),
      TimeStarted = startTime.ToTimestamp(),
      Timezone = TimeZoneInfo.Local.DisplayName,
      LocaltimeOffset = DateTimeOffset.Now.Offset.ToString()
    };
}
