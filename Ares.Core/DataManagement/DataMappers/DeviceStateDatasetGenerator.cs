using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.DataManagement.DataMappers;

public class DeviceStateDatasetGenerator(
  IDeviceStateGetter _deviceStateGetter,
  IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  private const string TimestampColumnName = "Timestamp";
  private const string CampaignColumnName = "Campaign";
  private const string ExperimentNumberColumnName = "Experiment Number";
  private const string StepNameColumnName = "Step Name";
  private const string DynamicColumnPrefix = "Data.";

  public async ValueTask<AresDataset[]> GenerateAsync(DeviceStateRequestFilter filter, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var stateMaps = await _deviceStateGetter.GetStates<DeviceState>(filter, cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    var campaignRecords = await GetCampaignRecords(filter, cancellationToken);
    var datasets = new List<AresDataset>();

    foreach(var stateMap in stateMaps)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var states = stateMap.Value.OrderBy(state => state.Timestamp).ToArray();
      var columns = CreateColumns(states);
      var dataset = new AresDataset
      {
        Name = stateMap.Key
      };
      dataset.Columns.AddRange(columns);
      dataset.Rows.AddRange(CreateRows(states, campaignRecords, filter, cancellationToken));
      datasets.Add(dataset);
    }

    return datasets.ToArray();
  }

  private static IEnumerable<AresDataColumn> CreateColumns(IEnumerable<DeviceState> states)
  {
    var columns = states
      .SelectMany(state => state.Data?.Fields ?? [])
      .SelectMany(field => AresValueFlattener.Flatten(GetColumnName(field.Key), field.Value))
      .GroupBy(field => field.Key)
      .OrderBy(group => group.Key)
      .Select(group => new AresDataColumn
      {
        Name = group.Key,
        Schema = group.First().Value.ToAresValueSchema()
      })
      .ToArray();

    foreach(var column in columns)
    {
      column.Schema.Optional = true;
    }

    return
    [
      new AresDataColumn
      {
        Name = TimestampColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Timestamp }
      },
      CreateOptionalColumn(CampaignColumnName, AresDataType.String),
      CreateOptionalColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateOptionalColumn(StepNameColumnName, AresDataType.String),
      .. columns
    ];
  }

  private static IEnumerable<AresDataRow> CreateRows(
    DeviceState[] states,
    CampaignRecord[] campaignRecords,
    DeviceStateRequestFilter filter,
    CancellationToken cancellationToken)
  {
    var minimumSampleInterval = filter.Interval?.ToTimeSpan() ?? default;
    if(minimumSampleInterval.TotalMilliseconds < 1)
    {
      return states.SelectMany(state => CreateRows(state, campaignRecords, cancellationToken)).ToArray();
    }

    if(states.Length == 0)
    {
      return [];
    }

    var rows = new List<AresDataRow>();
    var lastIncludedTimestamp = states.First().Timestamp.ToDateTime();
    rows.AddRange(CreateRows(states.First(), campaignRecords, cancellationToken));

    foreach(var state in states.Skip(1))
    {
      cancellationToken.ThrowIfCancellationRequested();
      var timestamp = state.Timestamp.ToDateTime();
      if(timestamp - lastIncludedTimestamp >= minimumSampleInterval)
      {
        rows.AddRange(CreateRows(state, campaignRecords, cancellationToken));
        lastIncludedTimestamp = timestamp;
      }
    }

    return rows;
  }

  private static IEnumerable<AresDataRow> CreateRows(
    DeviceState state,
    CampaignRecord[] campaignRecords,
    CancellationToken cancellationToken)
  {
    var activeCampaigns = campaignRecords
      .Where(campaign => Contains(campaign.TimeStarted, campaign.TimeFinished, state.Timestamp))
      .ToArray();

    if(activeCampaigns.Length == 0)
      return [CreateRow(state, null, cancellationToken)];

    return activeCampaigns.Select(campaign => CreateRow(state, campaign, cancellationToken)).ToArray();
  }

  private static AresDataRow CreateRow(DeviceState state, CampaignRecord? campaign, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    data.Fields[TimestampColumnName] = AresValueHelper.CreateTimestamp(state.Timestamp);

    if(campaign is not null)
    {
      data.Fields[CampaignColumnName] = AresValueHelper.CreateString(campaign.Name);
      var experiment = campaign.Experiments
        .Where(experimentRecord => Contains(experimentRecord.TimeStarted, experimentRecord.TimeFinished, state.Timestamp))
        .OrderByDescending(experimentRecord => experimentRecord.TimeStarted)
        .FirstOrDefault();
      if(experiment is not null)
      {
        data.Fields[ExperimentNumberColumnName] = AresValueHelper.CreateInt(experiment.Number);
        var step = experiment.Steps
          .Where(stepRecord => Contains(stepRecord.TimeStarted, stepRecord.TimeFinished, state.Timestamp))
          .OrderByDescending(stepRecord => stepRecord.TimeStarted)
          .FirstOrDefault();
        if(step is not null && !string.IsNullOrWhiteSpace(step.Name))
          data.Fields[StepNameColumnName] = AresValueHelper.CreateString(step.Name);
      }
    }

    foreach(var field in state.Data?.Fields ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      foreach(var flattenedField in AresValueFlattener.Flatten(GetColumnName(field.Key), field.Value))
      {
        cancellationToken.ThrowIfCancellationRequested();
        data.Fields[flattenedField.Key] = flattenedField.Value.Clone();
      }
    }

    return new AresDataRow
    {
      Data = data
    };
  }

  private async Task<CampaignRecord[]> GetCampaignRecords(DeviceStateRequestFilter filter, CancellationToken cancellationToken)
  {
    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    var query = context.CampaignExecutionSummaries
      .AsNoTracking()
      .IgnoreAutoIncludes()
      .Include(summary => summary.ExecutionInfo)
      .Include(summary => summary.ExperimentSummaries)
        .ThenInclude(experiment => experiment.ExecutionInfo)
      .Include(summary => summary.ExperimentSummaries)
        .ThenInclude(experiment => experiment.StepSummaries)
        .ThenInclude(step => step.ExecutionInfo)
      .Include(summary => summary.ExperimentSummaries)
        .ThenInclude(experiment => experiment.ExperimentOverview)
        .ThenInclude(overview => overview.Template)
        .ThenInclude(template => template.StepTemplates)
      .AsSplitQuery();

    if(!string.IsNullOrWhiteSpace(filter.CompletedCampaignId))
    {
      query = query.Where(summary =>
        summary.UniqueId == filter.CompletedCampaignId ||
        summary.CampaignId == filter.CompletedCampaignId);
    }

    if(!string.IsNullOrWhiteSpace(filter.CompletedExperimentId))
    {
      query = query.Where(summary => summary.ExperimentSummaries.Any(experiment =>
        experiment.ExperimentOverview.UniqueId == filter.CompletedExperimentId));
    }

    if(filter.Start is not null)
      query = query.Where(summary => summary.ExecutionInfo.TimeFinished >= filter.Start);

    if(filter.End is not null)
      query = query.Where(summary => summary.ExecutionInfo.TimeStarted <= filter.End);

    var summaries = await query.ToArrayAsync(cancellationToken);
    return summaries
      .Where(summary =>
        summary.ExecutionInfo?.TimeStarted is not null &&
        summary.ExecutionInfo.TimeFinished is not null)
      .Select(CreateCampaignRecord)
      .OrderBy(record => record.TimeStarted)
      .ToArray();
  }

  private static CampaignRecord CreateCampaignRecord(CampaignExecutionSummary summary)
  {
    var experiments = summary.ExperimentSummaries
      .OrderBy(experiment => experiment.ExecutionInfo?.TimeStarted)
      .Select((experiment, index) => new { Experiment = experiment, Number = index + 1 })
      .Where(item =>
        item.Experiment.ExecutionInfo?.TimeStarted is not null &&
        item.Experiment.ExecutionInfo.TimeFinished is not null)
      .Select(item => CreateExperimentRecord(item.Experiment, item.Number))
      .ToArray();

    return new CampaignRecord(
      summary.CampaignName,
      summary.ExecutionInfo.TimeStarted,
      summary.ExecutionInfo.TimeFinished,
      experiments);
  }

  private static ExperimentRecord CreateExperimentRecord(ExperimentExecutionSummary experiment, int number)
  {
    var stepTemplates = (experiment.ExperimentOverview?.Template?.StepTemplates ?? []).ToArray();
    var steps = experiment.StepSummaries
      .OrderBy(step => step.ExecutionInfo?.TimeStarted)
      .Select((step, index) => new { Step = step, Index = index })
      .Where(item =>
        item.Step.ExecutionInfo?.TimeStarted is not null &&
        item.Step.ExecutionInfo.TimeFinished is not null)
      .Select(item => new StepRecord(
        GetStepName(item.Step, stepTemplates, item.Index),
        item.Step.ExecutionInfo.TimeStarted,
        item.Step.ExecutionInfo.TimeFinished))
      .ToArray();

    return new ExperimentRecord(
      number,
      experiment.ExecutionInfo.TimeStarted,
      experiment.ExecutionInfo.TimeFinished,
      steps);
  }

  private static string? GetStepName(StepExecutionSummary step, StepTemplate[] stepTemplates, int index)
  {
    var matchingTemplate = stepTemplates.FirstOrDefault(template => template.UniqueId == step.StepId);
    return matchingTemplate?.Name ?? stepTemplates.ElementAtOrDefault(index)?.Name;
  }

  private static bool Contains(Timestamp start, Timestamp finish, Timestamp timestamp)
  {
    return timestamp >= start && timestamp <= finish;
  }

  private static AresDataColumn CreateOptionalColumn(string name, AresDataType type)
  {
    return new AresDataColumn
    {
      Name = name,
      Schema = new AresValueSchema { Type = type, Optional = true }
    };
  }

  private static string GetColumnName(string fieldName)
  {
    return fieldName is TimestampColumnName or CampaignColumnName or ExperimentNumberColumnName or StepNameColumnName
      ? $"{DynamicColumnPrefix}{fieldName}"
      : fieldName;
  }

  private record CampaignRecord(string Name, Timestamp TimeStarted, Timestamp TimeFinished, ExperimentRecord[] Experiments);

  private record ExperimentRecord(int Number, Timestamp TimeStarted, Timestamp TimeFinished, StepRecord[] Steps);

  private record StepRecord(string? Name, Timestamp TimeStarted, Timestamp TimeFinished);
}
