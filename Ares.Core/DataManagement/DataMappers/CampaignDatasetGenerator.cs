using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.DataManagement.DataMappers;

public class CampaignDatasetGenerator(IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  private const string ExperimentNumberColumnName = "Experiment Number";
  private const string ExperimentTemplateColumnName = "Experiment Template";
  private const string StepNumberColumnName = "Step Number";
  private const string CommandNumberColumnName = "Command Number";
  private const string CommandNameColumnName = "Command Name";
  private const string CommandDescriptionColumnName = "Command Description";
  private const string TimeStartedColumnName = "Time Started";
  private const string TimeFinishedColumnName = "Time Finished";
  private const string DurationSecondsColumnName = "Duration Seconds";
  private const string AnalysisResultColumnName = "Analysis Result";
  private const string StatusColumnName = "Status";
  private const string SuccessColumnName = "Success";
  private const string ErrorColumnName = "Error";
  private const string InputColumnPrefix = "Input.";
  private const string OutputColumnPrefix = "Output.";
  private const string OutputColumnName = "Output";

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

    return [
      CreateExperimentsDataset(experiments, cancellationToken),
      CreateCommandsDataset(experiments, cancellationToken)
    ];
  }

  private static AresDataset CreateExperimentsDataset(ExperimentExecutionSummary[] experiments, CancellationToken cancellationToken)
  {
    var dataset = new AresDataset
    {
      Name = "Experiments"
    };

    dataset.Columns.AddRange(CreateExperimentColumns(experiments, cancellationToken));
    dataset.Rows.AddRange(experiments.Select((experiment, index) => CreateExperimentRow(experiment, index + 1, cancellationToken)));
    return dataset;
  }

  private static AresDataset CreateCommandsDataset(ExperimentExecutionSummary[] experiments, CancellationToken cancellationToken)
  {
    var commandRecords = CreateCommandRecords(experiments, cancellationToken).ToArray();
    var dataset = new AresDataset
    {
      Name = "Commands"
    };

    dataset.Columns.AddRange(CreateCommandColumns(commandRecords, cancellationToken));
    dataset.Rows.AddRange(commandRecords.Select(record => CreateCommandRow(record, cancellationToken)));
    return dataset;
  }

  private static IEnumerable<AresDataColumn> CreateExperimentColumns(IEnumerable<ExperimentExecutionSummary> experiments, CancellationToken cancellationToken)
  {
    return
    [
      CreateColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateColumn(ExperimentTemplateColumnName, AresDataType.String, optional: true),
      CreateColumn(TimeStartedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(TimeFinishedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(DurationSecondsColumnName, AresDataType.Number, optional: true),
      CreateColumn(AnalysisResultColumnName, AresDataType.Number, optional: true),
      .. CreateExperimentDynamicColumns(experiments, cancellationToken)
    ];
  }

  private static IEnumerable<AresDataColumn> CreateCommandColumns(IEnumerable<CommandRecord> commandRecords, CancellationToken cancellationToken)
  {
    return
    [
      CreateColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateColumn(StepNumberColumnName, AresDataType.Int),
      CreateColumn(CommandNumberColumnName, AresDataType.Int),
      CreateColumn(CommandNameColumnName, AresDataType.String, optional: true),
      CreateColumn(CommandDescriptionColumnName, AresDataType.String, optional: true),
      CreateColumn(TimeStartedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(TimeFinishedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(DurationSecondsColumnName, AresDataType.Number, optional: true),
      CreateColumn(StatusColumnName, AresDataType.String, optional: true),
      CreateColumn(SuccessColumnName, AresDataType.Boolean, optional: true),
      CreateColumn(ErrorColumnName, AresDataType.String, optional: true),
      .. CreateCommandDynamicColumns(commandRecords, cancellationToken)
    ];
  }

  private static IEnumerable<AresDataColumn> CreateExperimentDynamicColumns(IEnumerable<ExperimentExecutionSummary> experiments, CancellationToken cancellationToken)
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

  private static IEnumerable<AresDataColumn> CreateCommandDynamicColumns(IEnumerable<CommandRecord> commandRecords, CancellationToken cancellationToken)
  {
    var columns = new Dictionary<string, AresValueSchema>();

    foreach(var commandRecord in commandRecords)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var output = commandRecord.Command.Result?.Result;
      if(output is null || output.KindCase == AresValue.KindOneofCase.None)
        continue;

      foreach(var flattenedField in AresValueFlattener.Flatten(OutputColumnName, output))
      {
        cancellationToken.ThrowIfCancellationRequested();
        TryAddDynamicColumn(columns, flattenedField.Key, flattenedField.Value);
      }
    }

    return columns.Select(column => new AresDataColumn
    {
      Name = column.Key,
      Schema = column.Value
    });
  }

  private static IEnumerable<CommandRecord> CreateCommandRecords(IEnumerable<ExperimentExecutionSummary> experiments, CancellationToken cancellationToken)
  {
    foreach(var experimentItem in experiments.Select((experiment, index) => new { Experiment = experiment, ExperimentNumber = index + 1 }))
    {
      cancellationToken.ThrowIfCancellationRequested();

      var steps = experimentItem.Experiment.StepSummaries
        .OrderBy(step => step.ExecutionInfo?.TimeStarted)
        .ToArray();

      foreach(var stepItem in steps.Select((step, index) => new { Step = step, StepNumber = index + 1 }))
      {
        cancellationToken.ThrowIfCancellationRequested();

        var commands = stepItem.Step.CommandSummaries
          .OrderBy(command => command.ExecutionInfo?.TimeStarted)
          .ToArray();

        foreach(var commandItem in commands.Select((command, index) => new { Command = command, CommandNumber = index + 1 }))
        {
          cancellationToken.ThrowIfCancellationRequested();
          yield return new CommandRecord(
            experimentItem.Experiment,
            experimentItem.ExperimentNumber,
            stepItem.Step,
            stepItem.StepNumber,
            commandItem.Command,
            commandItem.CommandNumber);
        }
      }
    }
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

  private static AresDataRow CreateExperimentRow(ExperimentExecutionSummary experiment, int experimentNumber, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    AddExperimentFields(data, experiment, experimentNumber);
    AddString(data, ExperimentTemplateColumnName, experiment.ExperimentOverview?.Template?.Name);
    AddExecutionFields(data, experiment.ExecutionInfo);

    if(experiment.ExperimentOverview?.AnalysisOverview is not null)
      data.Fields[AnalysisResultColumnName] = AresValueHelper.CreateNumber(experiment.ExperimentOverview.AnalysisOverview.Result);

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

  private static AresDataRow CreateCommandRow(CommandRecord record, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var data = new AresStruct();
    AddExperimentFields(data, record.Experiment, record.ExperimentNumber);
    data.Fields[StepNumberColumnName] = AresValueHelper.CreateInt(record.StepNumber);
    data.Fields[CommandNumberColumnName] = AresValueHelper.CreateInt(record.CommandNumber);
    AddString(data, CommandNameColumnName, record.Command.CommandName);
    AddString(data, CommandDescriptionColumnName, record.Command.CommandDescription);
    AddExecutionFields(data, record.Command.ExecutionInfo);
    AddString(data, StatusColumnName, record.Command.StatusCode.ToString());

    if(record.Command.Result is not null)
    {
      data.Fields[SuccessColumnName] = AresValueHelper.CreateBool(record.Command.Result.Success);
      AddString(data, ErrorColumnName, record.Command.Result.Error);

      if(record.Command.Result.Result is not null && record.Command.Result.Result.KindCase != AresValue.KindOneofCase.None)
        AddFlattenedValue(data, OutputColumnName, record.Command.Result.Result, cancellationToken);
    }

    return new AresDataRow
    {
      Data = data
    };
  }

  private static AresDataColumn CreateColumn(string name, AresDataType type, bool optional = false)
  {
    return new AresDataColumn
    {
      Name = name,
      Schema = new AresValueSchema { Type = type, Optional = optional }
    };
  }

  private static void AddExperimentFields(AresStruct data, ExperimentExecutionSummary experiment, int experimentNumber)
  {
    data.Fields[ExperimentNumberColumnName] = AresValueHelper.CreateInt(experimentNumber);
  }

  private static void AddExecutionFields(AresStruct data, ExecutionInfo? executionInfo)
  {
    if(executionInfo?.TimeStarted is not null)
      data.Fields[TimeStartedColumnName] = AresValueHelper.CreateTimestamp(executionInfo.TimeStarted);

    if(executionInfo?.TimeFinished is not null)
      data.Fields[TimeFinishedColumnName] = AresValueHelper.CreateTimestamp(executionInfo.TimeFinished);

    if(executionInfo?.TimeStarted is not null && executionInfo.TimeFinished is not null)
    {
      var duration = executionInfo.TimeFinished.ToDateTime() - executionInfo.TimeStarted.ToDateTime();
      data.Fields[DurationSecondsColumnName] = AresValueHelper.CreateNumber(duration.TotalSeconds);
    }
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

  private record CommandRecord(
    ExperimentExecutionSummary Experiment,
    int ExperimentNumber,
    StepExecutionSummary Step,
    int StepNumber,
    CommandExecutionSummary Command,
    int CommandNumber);
}
