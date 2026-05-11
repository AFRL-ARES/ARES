using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.DataManagement.DataMappers;

internal class CampaignDatasetGenerator(IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  private const string ExperimentNumberColumnName = "Experiment Number";
  private const string TimeStartedColumnName = "Time Started";
  private const string TimeFinishedColumnName = "Time Finished";
  private const string AnalysisResultColumnName = "Analysis Result";
  private const string ParameterColumnPrefix = "Parameter.";

  public async ValueTask<AresDataset[]> GenerateAsync(string summaryId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    var summary = await ctx.CampaignExecutionSummaries.FirstOrDefaultAsync(ces => ces.UniqueId == summaryId, cancellationToken);
    if(summary is null)
      return [];

    var experiments = summary.ExperimentSummaries
      .OrderBy(experiment => experiment.ExecutionInfo?.TimeStarted)
      .ToArray();

    var dataset = new AresDataset
    {
      Name = GetDatasetName(summary, summaryId)
    };

    dataset.Columns.AddRange(CreateFixedColumns());
    dataset.Columns.AddRange(CreateDynamicColumns(experiments, cancellationToken));
    dataset.Rows.AddRange(experiments.Select((experiment, index) => CreateRow(experiment, index + 1, cancellationToken)));

    return [dataset];
  }

  private static string GetDatasetName(CampaignExecutionSummary summary, string summaryId)
  {
    if(!string.IsNullOrWhiteSpace(summary.CampaignName))
      return summary.CampaignName;

    if(!string.IsNullOrWhiteSpace(summary.CampaignId))
      return summary.CampaignId;

    return summaryId;
  }

  private static IEnumerable<AresDataColumn> CreateFixedColumns()
  {
    return
    [
      new AresDataColumn
      {
        Name = ExperimentNumberColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Int }
      },
      new AresDataColumn
      {
        Name = TimeStartedColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Timestamp }
      },
      new AresDataColumn
      {
        Name = TimeFinishedColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Timestamp }
      },
      new AresDataColumn
      {
        Name = AnalysisResultColumnName,
        Schema = new AresValueSchema { Type = AresDataType.Number, Optional = true }
      }
    ];
  }

  private static IEnumerable<AresDataColumn> CreateDynamicColumns(IEnumerable<ExperimentExecutionSummary> experiments, CancellationToken cancellationToken)
  {
    var columns = new Dictionary<string, AresValueSchema>();

    foreach(var experiment in experiments)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var resultFields = experiment.ExperimentOverview?.Result?.Fields.OrderBy(field => field.Key)
        ?? Enumerable.Empty<KeyValuePair<string, AresValue>>();
      foreach(var field in resultFields)
      {
        cancellationToken.ThrowIfCancellationRequested();
        TryAddDynamicColumn(columns, field.Key, field.Value);
      }

      var parameters = experiment.ExperimentOverview?.Parameters.OrderBy(GetParameterColumnName)
        ?? Enumerable.Empty<Parameter>();
      foreach(var parameter in parameters)
      {
        cancellationToken.ThrowIfCancellationRequested();
        TryAddDynamicColumn(columns, GetParameterColumnName(parameter), parameter.Value);
      }
    }

    return columns.Select(column => new AresDataColumn
    {
      Name = column.Key,
      Schema = column.Value
    });
  }

  private static void TryAddDynamicColumn(IDictionary<string, AresValueSchema> columns, string columnName, AresValue? value)
  {
    if(value is null || columns.ContainsKey(columnName))
      return;

    var schema = value.ToAresValueSchema();
    schema.Optional = true;
    columns[columnName] = schema;
  }

  private static AresDataRow CreateRow(ExperimentExecutionSummary experiment, int experimentNumber, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    data.Fields[ExperimentNumberColumnName] = AresValueHelper.CreateInt(experimentNumber);

    if(experiment.ExecutionInfo?.TimeStarted is not null)
      data.Fields[TimeStartedColumnName] = AresValueHelper.CreateTimestamp(experiment.ExecutionInfo.TimeStarted);

    if(experiment.ExecutionInfo?.TimeFinished is not null)
      data.Fields[TimeFinishedColumnName] = AresValueHelper.CreateTimestamp(experiment.ExecutionInfo.TimeFinished);

    if(experiment.ExperimentOverview?.AnalysisOverview is not null)
      data.Fields[AnalysisResultColumnName] = AresValueHelper.CreateNumber(experiment.ExperimentOverview.AnalysisOverview.Result);

    foreach(var field in experiment.ExperimentOverview?.Result?.Fields ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      data.Fields[field.Key] = field.Value.Clone();
    }

    foreach(var parameter in experiment.ExperimentOverview?.Parameters ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();

      if(parameter.Value is not null)
        data.Fields[GetParameterColumnName(parameter)] = parameter.Value.Clone();
    }

    return new AresDataRow
    {
      Data = data
    };
  }

  private static string GetParameterColumnName(Parameter parameter)
  {
    if(!string.IsNullOrWhiteSpace(parameter.Metadata?.Name))
      return $"{ParameterColumnPrefix}{parameter.Metadata.Name}";

    if(!string.IsNullOrWhiteSpace(parameter.UniqueId))
      return $"{ParameterColumnPrefix}{parameter.UniqueId}";

    return $"{ParameterColumnPrefix}{parameter.Index}";
  }
}
