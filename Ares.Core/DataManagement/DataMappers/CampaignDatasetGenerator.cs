using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.DataManagement.DataMappers;

public class CampaignDatasetGenerator(IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  private const string ExperimentNumberColumnName = "Experiment Number";
  private const string ExperimentExecutionIdColumnName = "Experiment Execution ID";
  private const string ExperimentIdColumnName = "Experiment ID";
  private const string ExperimentTemplateColumnName = "Experiment Template";
  private const string TimeStartedColumnName = "Time Started";
  private const string TimeFinishedColumnName = "Time Finished";
  private const string DurationSecondsColumnName = "Duration Seconds";
  private const string AnalysisResultColumnName = "Analysis Result";
  private const string ResultOutputPathColumnName = "Result Output Path";
  private const string InputColumnPrefix = "Input.";
  private const string OutputColumnPrefix = "Output.";

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
      Name = "Experiments"
    };

    dataset.Columns.AddRange(CreateFixedColumns());
    dataset.Columns.AddRange(CreateDynamicColumns(experiments, cancellationToken));
    dataset.Rows.AddRange(experiments.Select((experiment, index) => CreateRow(experiment, index + 1, cancellationToken)));

    return [dataset];
  }

  private static IEnumerable<AresDataColumn> CreateFixedColumns()
  {
    return
    [
      CreateColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateColumn(ExperimentExecutionIdColumnName, AresDataType.String, optional: true),
      CreateColumn(ExperimentIdColumnName, AresDataType.String, optional: true),
      CreateColumn(ExperimentTemplateColumnName, AresDataType.String, optional: true),
      CreateColumn(TimeStartedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(TimeFinishedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(DurationSecondsColumnName, AresDataType.Number, optional: true),
      CreateColumn(AnalysisResultColumnName, AresDataType.Number, optional: true),
      CreateColumn(ResultOutputPathColumnName, AresDataType.String, optional: true)
    ];
  }

  private static AresDataColumn CreateColumn(string name, AresDataType type, bool optional = false)
  {
    return new AresDataColumn
    {
      Name = name,
      Schema = new AresValueSchema { Type = type, Optional = optional }
    };
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
        foreach(var flattenedField in AresValueFlattener.Flatten($"{OutputColumnPrefix}{field.Key}", field.Value))
        {
          TryAddDynamicColumn(columns, flattenedField.Key, flattenedField.Value);
        }
      }

      var parameters = experiment.ExperimentOverview?.Parameters.OrderBy(GetParameterColumnName)
        ?? Enumerable.Empty<Parameter>();
      foreach(var parameter in parameters)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var parameterValue = parameter.GetValue();
        if(parameterValue is null)
          continue;

        foreach(var flattenedField in AresValueFlattener.Flatten(GetParameterColumnName(parameter), parameterValue))
        {
          TryAddDynamicColumn(columns, flattenedField.Key, flattenedField.Value);
        }
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
    if(value is null)
      return;

    var schema = value.ToAresValueSchema();
    schema.Optional = true;
    if(!columns.TryGetValue(columnName, out var existingSchema))
    {
      columns[columnName] = schema;
      return;
    }

    if(existingSchema.Type != schema.Type)
      existingSchema.Type = AresDataType.Any;
  }

  private static AresDataRow CreateRow(ExperimentExecutionSummary experiment, int experimentNumber, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    data.Fields[ExperimentNumberColumnName] = AresValueHelper.CreateInt(experimentNumber);

    AddString(data, ExperimentExecutionIdColumnName, experiment.HasUniqueId ? experiment.UniqueId : null);
    AddString(data, ExperimentIdColumnName, experiment.ExperimentId);
    AddString(data, ExperimentTemplateColumnName, experiment.ExperimentOverview?.Template?.Name);

    if(experiment.ExecutionInfo?.TimeStarted is not null)
      data.Fields[TimeStartedColumnName] = AresValueHelper.CreateTimestamp(experiment.ExecutionInfo.TimeStarted);

    if(experiment.ExecutionInfo?.TimeFinished is not null)
      data.Fields[TimeFinishedColumnName] = AresValueHelper.CreateTimestamp(experiment.ExecutionInfo.TimeFinished);

    if(experiment.ExecutionInfo?.TimeStarted is not null && experiment.ExecutionInfo.TimeFinished is not null)
    {
      var duration = experiment.ExecutionInfo.TimeFinished.ToDateTime() - experiment.ExecutionInfo.TimeStarted.ToDateTime();
      data.Fields[DurationSecondsColumnName] = AresValueHelper.CreateNumber(duration.TotalSeconds);
    }

    if(experiment.ExperimentOverview?.AnalysisOverview is not null)
      data.Fields[AnalysisResultColumnName] = AresValueHelper.CreateNumber(experiment.ExperimentOverview.AnalysisOverview.Result);

    AddString(data, ResultOutputPathColumnName, experiment.ResultOutputPath);

    foreach(var field in experiment.ExperimentOverview?.Result?.Fields ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      AddFlattenedValue(data, $"{OutputColumnPrefix}{field.Key}", field.Value, cancellationToken);
    }

    foreach(var parameter in experiment.ExperimentOverview?.Parameters ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();

      var parameterValue = parameter.GetValue();
      if(parameterValue is not null)
        AddFlattenedValue(data, GetParameterColumnName(parameter), parameterValue, cancellationToken);
    }

    return new AresDataRow
    {
      Data = data
    };
  }

  private static void AddString(AresStruct data, string columnName, string? value)
  {
    if(!string.IsNullOrWhiteSpace(value))
      data.Fields[columnName] = AresValueHelper.CreateString(value);
  }

  private static void AddFlattenedValue(AresStruct data, string columnName, AresValue value, CancellationToken cancellationToken)
  {
    foreach(var flattenedField in AresValueFlattener.Flatten(columnName, value))
    {
      cancellationToken.ThrowIfCancellationRequested();
      data.Fields[flattenedField.Key] = flattenedField.Value.Clone();
    }
  }

  private static string GetParameterColumnName(Parameter parameter)
  {
    if(!string.IsNullOrWhiteSpace(parameter.Metadata?.Name))
      return $"{InputColumnPrefix}{parameter.Metadata.Name}";

    if(!string.IsNullOrWhiteSpace(parameter.UniqueId))
      return $"{InputColumnPrefix}{parameter.UniqueId}";

    return $"{InputColumnPrefix}{parameter.Index}";
  }
}
