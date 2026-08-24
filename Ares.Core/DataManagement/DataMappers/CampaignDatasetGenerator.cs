using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
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
  private const string OutputVariableNameColumnName = "Output Variable Name";
  private const string TimeStartedColumnName = "Time Started";
  private const string TimeFinishedColumnName = "Time Finished";
  private const string DurationSecondsColumnName = "Duration Seconds";
  private const string StatusColumnName = "Status";
  private const string SuccessColumnName = "Success";
  private const string ErrorColumnName = "Error";
  private const string PlanNumberColumnName = "Plan Number";
  private const string PlannerNameColumnName = "Planner Name";
  private const string PlannerTypeColumnName = "Planner Type";
  private const string PlannerVersionColumnName = "Planner Version";
  private const string AnalyzerNameColumnName = "Analyzer Name";
  private const string AnalyzerTypeColumnName = "Analyzer Type";
  private const string AnalyzerVersionColumnName = "Analyzer Version";
  private const string TimeRequestSentColumnName = "Time Request Sent";
  private const string TimeResponseReceivedColumnName = "Time Response Received";
  private const string OutcomeColumnName = "Outcome";
  private const string ObjectiveStatusColumnName = "Objective Status";
  private const string ResultColumnName = "Objective.Result";
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
    var experimentNumbers = experiments
      .Select((experiment, index) => new { experiment.ExperimentId, ExperimentNumber = index + 1 })
      .Where(item => !string.IsNullOrWhiteSpace(item.ExperimentId))
      .GroupBy(item => item.ExperimentId)
      .ToDictionary(group => group.Key, group => group.First().ExperimentNumber);
    var plannerRecords = new List<PlannerRecord>();
    var analyzerRecords = new List<AnalyzerRecord>();
    var campaignStart = summary.ExecutionInfo?.TimeStarted;
    var campaignEnd = summary.ExecutionInfo?.TimeFinished;
    if(campaignStart is not null && campaignEnd is not null)
    {
      try
      {
        var plannerTransactions = await ctx.PlannerTransactions
          .Where(transaction => transaction.TimeRequestSent >= campaignStart && transaction.TimeResponseReceived <= campaignEnd)
          .ToListAsync(cancellationToken);

        foreach(var transaction in plannerTransactions)
        {
          cancellationToken.ThrowIfCancellationRequested();
          if(TryCreatePlannerRecord(transaction, summary, experimentNumbers, out var record))
            plannerRecords.Add(record);
        }
      }
      catch(Exception e)
      {
        throw e;
      }



      var analyzerTransactions = await ctx.AnalyzerTransactions
        .Where(transaction => transaction.TimeRequestSent >= campaignStart && transaction.TimeResponseReceived <= campaignEnd)
        .ToListAsync(cancellationToken);
      foreach(var transaction in analyzerTransactions)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if(TryCreateAnalyzerRecord(transaction, summary, experimentNumbers, out var record))
          analyzerRecords.Add(record);
      }
    }

    return [
      CreateExperimentsDataset(experiments, cancellationToken),
      CreateCommandsDataset(experiments, cancellationToken),
      CreatePlannerTransactionsDataset(plannerRecords.OrderBy(record => record.Transaction.TimeRequestSent).ToArray(), cancellationToken),
      CreateAnalyzerTransactionsDataset(analyzerRecords.OrderBy(record => record.Transaction.TimeRequestSent).ToArray(), cancellationToken)
    ];
  }

  private static AresDataset CreateExperimentsDataset(ExperimentExecutionSummary[] experiments, CancellationToken cancellationToken)
  {
    var dataset = new AresDataset
    {
      Name = "Experiments"
    };

    var createdColumns = CreateExperimentColumns(experiments, cancellationToken);
    dataset.Columns.AddRange(createdColumns);
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

  private static AresDataset CreatePlannerTransactionsDataset(PlannerRecord[] records, CancellationToken cancellationToken)
  {
    var dataset = new AresDataset
    {
      Name = "Planner Transactions"
    };

    dataset.Columns.AddRange(CreatePlannerTransactionColumns(records, cancellationToken));
    dataset.Rows.AddRange(records.SelectMany(record => CreatePlannerTransactionRows(record, cancellationToken)));

    return dataset;
  }

  private static AresDataset CreateAnalyzerTransactionsDataset(AnalyzerRecord[] records, CancellationToken cancellationToken)
  {
    var dataset = new AresDataset
    {
      Name = "Analyzer Transactions"
    };

    dataset.Columns.AddRange(CreateAnalyzerTransactionColumns(records, cancellationToken));
    dataset.Rows.AddRange(records.Select(record => CreateAnalyzerTransactionRow(record, cancellationToken)));
    return dataset;
  }

  private static IEnumerable<AresDataColumn> CreateExperimentColumns(IEnumerable<ExperimentExecutionSummary> experiments, CancellationToken cancellationToken)
  {
    var columns = new List<AresDataColumn>
    {
      CreateColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateColumn(ExperimentTemplateColumnName, AresDataType.String, optional: true),
      CreateColumn(TimeStartedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(TimeFinishedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(DurationSecondsColumnName, AresDataType.Number, optional: true)
    };

    var dynamicColumns = CreateExperimentDynamicColumns(experiments, cancellationToken);
    columns.AddRange(dynamicColumns);
    return columns;
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
      CreateColumn(OutputVariableNameColumnName, AresDataType.String, optional: true),
      CreateColumn(TimeStartedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(TimeFinishedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(DurationSecondsColumnName, AresDataType.Number, optional: true),
      CreateColumn(StatusColumnName, AresDataType.String, optional: true),
      CreateColumn(SuccessColumnName, AresDataType.Boolean, optional: true),
      CreateColumn(ErrorColumnName, AresDataType.String, optional: true),
      .. CreateCommandInputColumns(commandRecords, cancellationToken),
      .. CreateCommandOutputColumns(commandRecords, cancellationToken)
    ];
  }

  private static IEnumerable<AresDataColumn> CreatePlannerTransactionColumns(IEnumerable<PlannerRecord> records, CancellationToken cancellationToken)
  {
    return
    [
      CreateColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateColumn(PlanNumberColumnName, AresDataType.Int, optional: true),
      CreateColumn(PlannerNameColumnName, AresDataType.String, optional: true),
      CreateColumn(PlannerTypeColumnName, AresDataType.String, optional: true),
      CreateColumn(PlannerVersionColumnName, AresDataType.String, optional: true),
      .. CreateTransactionTimingColumns(),
      CreateColumn(OutcomeColumnName, AresDataType.String, optional: true),
      CreateColumn(ErrorColumnName, AresDataType.String, optional: true),
      .. CreatePlannerDynamicColumns(records, cancellationToken)
    ];
  }

  private static IEnumerable<AresDataColumn> CreateAnalyzerTransactionColumns(IEnumerable<AnalyzerRecord> records, CancellationToken cancellationToken)
  {
    return
    [
      CreateColumn(ExperimentNumberColumnName, AresDataType.Int),
      CreateColumn(AnalyzerNameColumnName, AresDataType.String, optional: true),
      CreateColumn(AnalyzerTypeColumnName, AresDataType.String, optional: true),
      CreateColumn(AnalyzerVersionColumnName, AresDataType.String, optional: true),
      .. CreateTransactionTimingColumns(),
      CreateColumn(ResultColumnName, AresDataType.Number, optional: true),
      CreateColumn(OutcomeColumnName, AresDataType.String, optional: true),
      CreateColumn(ErrorColumnName, AresDataType.String, optional: true),
      .. CreateAnalyzerDynamicColumns(records, cancellationToken)
    ];
  }

  private static IEnumerable<AresDataColumn> CreateTransactionTimingColumns()
  {
    return
    [
      CreateColumn(TimeRequestSentColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(TimeResponseReceivedColumnName, AresDataType.Timestamp, optional: true),
      CreateColumn(DurationSecondsColumnName, AresDataType.Number, optional: true)
    ];
  }

  private static IEnumerable<AresDataColumn> CreateExperimentDynamicColumns(IEnumerable<ExperimentExecutionSummary> experiments, CancellationToken cancellationToken)
  {
    var columns = new Dictionary<string, AresValueSchema>();

    foreach(var experiment in experiments)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if(experiment.ExperimentOverview?.AnalysisOverview is not null)
      {
        if(experiment.ExperimentOverview.AnalysisOverview.Objectives.Any())
        {
          foreach(var objective in experiment.ExperimentOverview.AnalysisOverview.Objectives)
          {
            cancellationToken.ThrowIfCancellationRequested();
            TryAddDynamicColumn(columns, $"Objective.{objective.ObjectiveName}", objective.ObjectiveValue);
          }
        }

        // Handle the presence of deprecated result values gracefully
        else if(experiment.ExperimentOverview.AnalysisOverview.AnalyzerInfo.Name != "NONE")
        {
          var aresValueResult = AresValueHelper.CreateNumber(experiment.ExperimentOverview.AnalysisOverview.Result);
          TryAddDynamicColumn(columns, $"Objective.Result", aresValueResult);
        }

      }

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

      var cleanedParameters = CleanParametersOfDuplicateNames(experiment, cancellationToken);

      foreach(var item in cleanedParameters)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var parameterValue = item.Item1.GetValue();

        if(parameterValue is null)
          continue;

        // Use item.UniqueName here instead of calling GetParameterColumnName again
        foreach(var flattenedField in AresValueFlattener.Flatten(item.Item2, parameterValue))
          TryAddDynamicColumn(columns, flattenedField.Key, flattenedField.Value);
      }
    }

    return columns.Select(column => new AresDataColumn
    {
      Name = column.Key,
      Schema = column.Value
    });
  }

  private static IEnumerable<AresDataColumn> CreateCommandInputColumns(IEnumerable<CommandRecord> commandRecords, CancellationToken cancellationToken)
  {
    var columns = new Dictionary<string, AresValueSchema>();

    foreach(var commandRecord in commandRecords)
    {
      foreach(var parameter in commandRecord.Template?.ArgumentBindings ?? [])
      {
        cancellationToken.ThrowIfCancellationRequested();
        var value = parameter.GetValue();
        if(value is not null)
          AddDynamicColumns(columns, GetParameterColumnName(parameter), value);
      }
    }

    return CreateDynamicColumns(columns);
  }

  private static IEnumerable<AresDataColumn> CreateCommandOutputColumns(IEnumerable<CommandRecord> commandRecords, CancellationToken cancellationToken)
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

  private static IEnumerable<AresDataColumn> CreatePlannerDynamicColumns(IEnumerable<PlannerRecord> records, CancellationToken cancellationToken)
  {
    var columns = new Dictionary<string, AresValueSchema>();

    foreach(var record in records)
    {
      var plans = record.Transaction.PlanningResponse?.Plans ?? Enumerable.Empty<Plan>();
      foreach(var plan in plans)
      {
        foreach(var parameter in plan.PlannedParameters ?? [])
        {
          cancellationToken.ThrowIfCancellationRequested();
          if(parameter.ParameterValue is not null)
            AddDynamicColumns(columns, $"{OutputColumnPrefix}{parameter.ParameterName}", parameter.ParameterValue);
        }
      }
    }

    return CreateDynamicColumns(columns);
  }

  private static IEnumerable<AresDataColumn> CreateAnalyzerDynamicColumns(IEnumerable<AnalyzerRecord> records, CancellationToken cancellationToken)
  {
    var columns = new Dictionary<string, AresValueSchema>();

    foreach(var record in records)
    {
      foreach(var input in record.Transaction.AnalysisRequest?.Inputs?.Fields ?? [])
      {
        cancellationToken.ThrowIfCancellationRequested();
        if(input.Value is not null)
          AddDynamicColumns(columns, $"{InputColumnPrefix}{input.Key}", input.Value);
      }
    }

    return CreateDynamicColumns(columns);
  }

  private static void AddDynamicColumns(IDictionary<string, AresValueSchema> columns, string columnName, AresValue value)
  {
    foreach(var flattenedField in AresValueFlattener.Flatten(columnName, value))
    {
      TryAddDynamicColumn(columns, flattenedField.Key, flattenedField.Value);
    }
  }

  private static IEnumerable<AresDataColumn> CreateDynamicColumns(IDictionary<string, AresValueSchema> columns)
  {
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
      var commandTemplates = CreateUniqueCommandTemplateMap(experimentItem.Experiment);

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
          commandTemplates.TryGetValue(commandItem.Command.TemplateId, out var commandTemplate);
          yield return new CommandRecord(
            experimentItem.Experiment,
            experimentItem.ExperimentNumber,
            stepItem.Step,
            stepItem.StepNumber,
            commandItem.Command,
            commandItem.CommandNumber,
            commandTemplate);
        }
      }
    }
  }

  private static IReadOnlyDictionary<string, CommandTemplate> CreateUniqueCommandTemplateMap(ExperimentExecutionSummary experiment)
  {
    return (experiment.ExperimentOverview?.Template?.StepTemplates ?? [])
      .SelectMany(step => step.CommandTemplates)
      .Where(template => !string.IsNullOrWhiteSpace(template.UniqueId))
      .GroupBy(template => template.UniqueId)
      .Where(group => group.Count() == 1)
      .ToDictionary(group => group.Key, group => group.Single());
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
    {
      if(experiment.ExperimentOverview.AnalysisOverview.Objectives.Any())
      {
        foreach(var objective in experiment.ExperimentOverview.AnalysisOverview.Objectives)
          AddFlattenedValue(data, $"Objective.{objective.ObjectiveName}", objective.ObjectiveValue, cancellationToken);
      }

      //If we have no listed objectives AND there is an Analyzer assigned, assume it's using the legacy standard
      else if(experiment.ExperimentOverview.AnalysisOverview.AnalyzerInfo.Name != "NONE")
      {
        var aresValueResult = AresValueHelper.CreateNumber(experiment.ExperimentOverview.AnalysisOverview.Result);
        AddFlattenedValue(data, $"Objective.Result", aresValueResult, cancellationToken);
      }

    }

    foreach(var field in experiment.ExperimentOverview?.Result?.Fields ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      AddFlattenedValue(data, $"{OutputColumnPrefix}{field.Key}", field.Value, cancellationToken);
    }

    var cleanedParameters = CleanParametersOfDuplicateNames(experiment, cancellationToken);

    foreach(var item in cleanedParameters)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var parameterValue = item.Item1.GetValue();

      if(parameterValue is null)
        continue;

      foreach(var flattenedField in AresValueFlattener.Flatten(item.Item2, parameterValue))
        AddFlattenedValue(data, flattenedField.Key, flattenedField.Value, cancellationToken);
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
    AddString(data, OutputVariableNameColumnName, record.Command.VarName);
    AddExecutionFields(data, record.Command.ExecutionInfo);
    AddString(data, StatusColumnName, record.Command.StatusCode.ToString());

    foreach(var parameter in record.Template?.ArgumentBindings ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      var value = parameter.GetValue();
      if(value is not null)
        AddFlattenedValue(data, GetParameterColumnName(parameter), value, cancellationToken);
    }

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

  private static IEnumerable<AresDataRow> CreatePlannerTransactionRows(PlannerRecord record, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var transaction = record.Transaction;
    var plans = transaction.PlanningResponse?.Plans;

    // If there are no plans, we still output the transaction metadata
    if(plans is null || plans.Count == 0)
    {
      yield return BuildPlanRow(record, null, 0, cancellationToken);
      yield break;
    }

    // Yield one row per plan
    int planIndex = 1;
    foreach(var plan in plans)
    {
      yield return BuildPlanRow(record, plan, planIndex++, cancellationToken);
    }
  }

  private static AresDataRow BuildPlanRow(PlannerRecord record, Plan? plan, int planIndex, CancellationToken cancellationToken)
  {
    var transaction = record.Transaction;
    var data = new AresStruct();

    data.Fields[ExperimentNumberColumnName] = AresValueHelper.CreateInt(record.ExperimentNumber);
    AddString(data, PlannerNameColumnName, transaction.PlannerName);
    AddString(data, PlannerTypeColumnName, transaction.PlannerType);
    AddString(data, PlannerVersionColumnName, transaction.PlannerVersion);
    

    AddTransactionTimingFields(data, transaction.TimeRequestSent, transaction.TimeResponseReceived);

    if(plan is not null)
    {
      AddString(data, OutcomeColumnName, plan.PlanningOutcome.ToString());
      AddString(data, ErrorColumnName, plan.ErrorString);
    }

    if(transaction.PlanningResponse is not null)
    {
      AddString(data, ObjectiveStatusColumnName, transaction.PlanningResponse.ObjectiveStatus.ToString());
    }

    // Base AnalysisData
    if(transaction.PlanningRequest?.AnalysisData.Count > 0)
    {
      foreach(var analysisDataEntry in transaction.PlanningRequest.AnalysisData)
      {
        foreach(var objective in analysisDataEntry.AnalysisObjectives)
          data.Fields[$"Objective.{objective.ObjectiveName}"] = objective.ObjectiveValue;
      }
    }

    // Plan-specific data
    if(plan is not null)
    {
      data.Fields["Plan Number"] = AresValueHelper.CreateInt(planIndex);
      AddString(data, OutcomeColumnName, plan.PlanningOutcome.ToString());
      AddString(data, ErrorColumnName, plan.ErrorString);

      foreach(var parameter in plan.PlannedParameters)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if(parameter.ParameterValue is not null)
          AddFlattenedValue(data, $"{OutputColumnPrefix}{parameter.ParameterName}", parameter.ParameterValue, cancellationToken);
      }
    }

    return new AresDataRow { Data = data };
  }

  private static AresDataRow CreateAnalyzerTransactionRow(AnalyzerRecord record, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var transaction = record.Transaction;
    var data = new AresStruct();
    data.Fields[ExperimentNumberColumnName] = AresValueHelper.CreateInt(record.ExperimentNumber);
    AddString(data, AnalyzerNameColumnName, transaction.AnalyzerName);
    AddString(data, AnalyzerTypeColumnName, transaction.AnalyzerType);
    AddString(data, AnalyzerVersionColumnName, transaction.AnalyzerVersion);
    AddTransactionTimingFields(data, transaction.TimeRequestSent, transaction.TimeResponseReceived);

    if(transaction.AnalysisResponse is not null)
    {
      var aresValResult = AresValueHelper.CreateNumber(transaction.AnalysisResponse.Result);
      AddFlattenedValue(data, "Objective.Result", aresValResult, cancellationToken);

      AddString(data, OutcomeColumnName, transaction.AnalysisResponse.AnalysisOutcome.ToString());
      AddString(data, ErrorColumnName, transaction.AnalysisResponse.ErrorString);
    }

    else if(transaction.AnalyzerResponse.Objectives.Any())
    {
      foreach(var objective in transaction.AnalyzerResponse.Objectives)
        AddFlattenedValue(data, $"Objective.{objective.ObjectiveName}", objective.ObjectiveValue, cancellationToken);

      AddString(data, OutcomeColumnName, transaction.AnalyzerResponse.AnalysisOutcome.ToString());
      AddString(data, ErrorColumnName, transaction.AnalyzerResponse.ErrorString);
    }

    foreach(var input in transaction.AnalysisRequest?.Inputs?.Fields ?? [])
    {
      cancellationToken.ThrowIfCancellationRequested();
      if(input.Value is not null)
        AddFlattenedValue(data, $"{InputColumnPrefix}{input.Key}", input.Value, cancellationToken);
    }

    return new AresDataRow { Data = data };
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

  private static void AddTransactionTimingFields(AresStruct data, Timestamp? requestSent, Timestamp? responseReceived)
  {
    if(requestSent is not null)
      data.Fields[TimeRequestSentColumnName] = AresValueHelper.CreateTimestamp(requestSent);

    if(responseReceived is not null)
      data.Fields[TimeResponseReceivedColumnName] = AresValueHelper.CreateTimestamp(responseReceived);

    if(requestSent is not null && responseReceived is not null)
    {
      var duration = responseReceived.ToDateTime() - requestSent.ToDateTime();
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

  private static IEnumerable<(Parameter, string)> CleanParametersOfDuplicateNames(ExperimentExecutionSummary experiment, CancellationToken cancellationToken)
  {
    var stepTemplates = experiment.ExperimentOverview?.Template?.StepTemplates ?? Enumerable.Empty<StepTemplate>();
    var globalParameters = experiment.ExperimentOverview?.Parameters ?? Enumerable.Empty<Parameter>();

    var hierarchicalParameters = stepTemplates
        .OrderBy(step => step.Index)
        .SelectMany(step => step.CommandTemplates
            .OrderBy(cmd => cmd.Index)
            .SelectMany(cmd => cmd.ArgumentBindings
                .Select((param, paramIndex) => new
                {
                  Parameter = param,
                  BaseName = GetParameterColumnName(param),
                  StepIndex = step.Index,
                  CommandIndex = cmd.Index,
                  BindingIndex = (long)paramIndex
                })))
        .ToList();

    var structuredParamSet = new HashSet<Parameter>(hierarchicalParameters.Select(x => x.Parameter));

    var unstructuredParameters = globalParameters
        .Where(p => !structuredParamSet.Contains(p))
        .Select((param, index) => new
        {
          Parameter = param,
          BaseName = GetParameterColumnName(param),
          StepIndex = -1L,
          CommandIndex = -1L,
          BindingIndex = (long)index
        });

    var finalOrderedParameters = hierarchicalParameters
        .Concat(unstructuredParameters)
        .OrderBy(x => x.BaseName)
        .ThenBy(x => x.StepIndex)
        .ThenBy(x => x.CommandIndex)
        .ThenBy(x => x.BindingIndex)
        .ToList();

    var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var uniqueParameters = new List<(Parameter Parameter, string UniqueName)>();

    foreach(var item in finalOrderedParameters)
    {
      cancellationToken.ThrowIfCancellationRequested();
      string uniqueName = item.BaseName;
      int suffix = 1;

      while(!seenNames.Add(uniqueName))
      {
        uniqueName = $"{item.BaseName}_{suffix}";
        suffix++;
      }

      uniqueParameters.Add((item.Parameter, uniqueName));
    }

    return uniqueParameters;
  }

  private static string GetParameterColumnName(Parameter parameter)
  {
    if(parameter.SourceCase == Parameter.SourceOneofCase.PlannedSource && parameter.PlannedSource is not null)
      return parameter.PlannedSource.PlanningMetadata.Name;

    if(!string.IsNullOrWhiteSpace(parameter.Metadata?.Name))
      return $"{InputColumnPrefix}{parameter.Metadata.Name}";

    if(!string.IsNullOrWhiteSpace(parameter.UniqueId))
      return $"{InputColumnPrefix}{parameter.UniqueId}";

    return $"{InputColumnPrefix}{parameter.Index}";
  }

  private static bool TryCreatePlannerRecord(
    PlannerTransaction transaction,
    CampaignExecutionSummary summary,
    IReadOnlyDictionary<string, int> experimentNumbers,
    out PlannerRecord record)
  {
    var metadata = transaction.PlanningRequest?.Metadata;
    if(!TryGetExperimentNumber(metadata, summary, experimentNumbers, transaction.TimeRequestSent, transaction.TimeResponseReceived, out var experimentNumber))
    {
      record = null!;
      return false;
    }

    record = new PlannerRecord(transaction, experimentNumber);
    return true;
  }

  private static bool TryCreateAnalyzerRecord(
    AnalyzerTransaction transaction,
    CampaignExecutionSummary summary,
    IReadOnlyDictionary<string, int> experimentNumbers,
    out AnalyzerRecord record)
  {
    var metadata = transaction.AnalysisRequest?.Metadata;
    if(!TryGetExperimentNumber(metadata, summary, experimentNumbers, transaction.TimeRequestSent, transaction.TimeResponseReceived, out var experimentNumber))
    {
      record = null!;
      return false;
    }

    record = new AnalyzerRecord(transaction, experimentNumber);
    return true;
  }

  private static bool TryGetExperimentNumber(
    RequestMetadata? metadata,
    CampaignExecutionSummary summary,
    IReadOnlyDictionary<string, int> experimentNumbers,
    Timestamp? requestSent,
    Timestamp? responseReceived,
    out int experimentNumber)
  {
    experimentNumber = 0;
    if(metadata is null ||
      metadata.CampaignId != summary.CampaignId ||
      !experimentNumbers.TryGetValue(metadata.ExperimentId, out experimentNumber))
      return false;

    var campaignStart = summary.ExecutionInfo?.TimeStarted;
    var campaignEnd = summary.ExecutionInfo?.TimeFinished;
    return campaignStart is not null &&
      campaignEnd is not null &&
      requestSent is not null &&
      responseReceived is not null &&
      requestSent.ToDateTime() >= campaignStart.ToDateTime() &&
      responseReceived.ToDateTime() <= campaignEnd.ToDateTime();
  }

  private record CommandRecord(
    ExperimentExecutionSummary Experiment,
    int ExperimentNumber,
    StepExecutionSummary Step,
    int StepNumber,
    CommandExecutionSummary Command,
    int CommandNumber,
    CommandTemplate? Template);

  private record PlannerRecord(PlannerTransaction Transaction, int ExperimentNumber);

  private record AnalyzerRecord(AnalyzerTransaction Transaction, int ExperimentNumber);
}
