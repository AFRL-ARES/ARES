using Ares.Core.Execution.ControlTokens;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using NCalc;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Execution.Executors;

public class LogicExecutor : IExecutor<CommandExecutionSummary, CommandExecutionStatus>
{
  private readonly LogicTemplate _template;
  IExecutor<CommandExecutionSummary, CommandExecutionStatus>? _trueBranch;
  IExecutor<CommandExecutionSummary, CommandExecutionStatus>? _falseBranch;
  private readonly BehaviorSubject<CommandExecutionStatus> _stateSubject;
  private readonly ISystemSettingsManager _systemSettingsManager;
  private readonly INotifier _notifier;

  public LogicExecutor(LogicTemplate template, 
    IExecutor<CommandExecutionSummary, CommandExecutionStatus>? trueBranch, 
    IExecutor<CommandExecutionSummary, CommandExecutionStatus>? falseBranch, 
    ISystemSettingsManager systemSettingsManager, 
    INotifier notifier)
  {
    _template = template;
    _trueBranch = trueBranch;
    _falseBranch = falseBranch;
    _systemSettingsManager = systemSettingsManager;
    _notifier = notifier;

    // Initialize state subject...
    var executionStatus = new CommandExecutionStatus
    {
      CommandId = Guid.NewGuid().ToString(),
      CommandName = "Logic Gate",
      DeviceName = "ARES",
      State = ExecutionState.Undefined
    };

    _stateSubject = new BehaviorSubject<CommandExecutionStatus>(executionStatus);
    ExperimentStatusObservable = _stateSubject.AsObservable();
  }

  public async Task<CommandExecutionSummary> Execute(ExecutionControlToken token)
  => await Execute(token, new Dictionary<string, AresValue>());

  public async Task<CommandExecutionSummary> Execute(ExecutionControlToken token, IReadOnlyDictionary<string, AresValue> variableScope)
  {
    var startTime = DateTime.Now;
    var conditionIsTrue = EvaluateExpression(_template.EvaluationExpression, variableScope);
    var branchExecutor = conditionIsTrue ? _trueBranch : _falseBranch;

    if(branchExecutor is not null)
    {
      var branchSummary = await branchExecutor.Execute(token, variableScope);

      if(branchSummary.StatusCode != CommandStatusCode.CommandSuccess) 
        return branchSummary;
    }

    return ExecutorSummaryHelpers.CreateCommandExecutionSummary(_template, null, startTime, DateTime.UtcNow);
  }

  private bool EvaluateExpression(string expression, IReadOnlyDictionary<string, AresValue> scope)
  {
    if(string.IsNullOrWhiteSpace(expression))
      throw new ArgumentException("Evaluation expression cannot be empty.");

    var ncalcExpr = new Expression(expression);

    ncalcExpr.EvaluateParameter += (name, args) =>
    {
      if(scope.TryGetValue(name, out var aresValue))
        args.Result = ExtractPrimitiveValue(aresValue);
      
      else
        throw new InvalidOperationException($"Variable '{name}' not found in current scope.");
    };

    try
    {
      var result = ncalcExpr.Evaluate();

      if(result is bool boolResult)
        return boolResult;
      

      throw new InvalidOperationException($"Expression '{expression}' evaluated to a {result?.GetType().Name}, expected a Boolean.");
    }
    catch(Exception ex)
    {
      // NCalc throws this for syntax errors in the string
      throw new InvalidOperationException($"Failed to parse logic expression: {ex.Message}", ex);
    }
  }

  private object ExtractPrimitiveValue(AresValue aresValue)
  {
    return aresValue.KindCase switch
    {
      AresValue.KindOneofCase.NumberValue => aresValue.NumberValue,
      AresValue.KindOneofCase.FloatValue => (double)aresValue.FloatValue, // Cast to double for easier math
      AresValue.KindOneofCase.IntValue => (double)aresValue.IntValue,     // Cast to double for easier math
      AresValue.KindOneofCase.StringValue => aresValue.StringValue,
      AresValue.KindOneofCase.BoolValue => aresValue.BoolValue,

      // Handle Nulls gracefully
      AresValue.KindOneofCase.NullValue => null!,

      // Arrays and complex structs can't easily be compared with >, <, == in simple strings
      _ => throw new NotSupportedException($"Cannot evaluate logic against a variable of type {aresValue.KindCase}")
    };
  }

  public CommandExecutionStatus Status => _stateSubject.Value;
  public IObservable<CommandExecutionStatus> ExperimentStatusObservable { get; }

}
