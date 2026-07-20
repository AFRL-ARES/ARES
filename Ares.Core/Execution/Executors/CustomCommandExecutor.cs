using Ares.Core.CustomCommands;
using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using AresScript.ScriptBuilding;
using AresScript.Symbols;

namespace Ares.Core.Execution.Executors;

public sealed class CustomCommandExecutor(
  ICustomCommandPersistenceService customCommandPersistenceService,
  BaseEnvironmentBuilder environmentBuilder)
{
  private const string ArgumentVariablePrefix = "__custom_command_argument_";
  private const string ResultVariableName = "__custom_command_result";

  public async Task<CommandResult> Execute(
    string customCommandId,
    IReadOnlyList<Parameter> argumentBindings,
    CancellationToken token)
  {
    if(!Guid.TryParse(customCommandId, out var commandId))
      return FailedResult($"Custom command id '{customCommandId}' is invalid.");

    var command = await customCommandPersistenceService.GetAsync(commandId);
    if(command is null)
      return FailedResult($"Custom command '{customCommandId}' was not found.");

    var bindingValues = ResolveBindings(command, argumentBindings, out var bindingError);
    if(bindingError is not null)
      return FailedResult(bindingError);

    var environment = await environmentBuilder.Build();
    for(var i = 0; i < bindingValues.Length; i++)
      environment.AssignVariable($"{ArgumentVariablePrefix}{i}", bindingValues[i]);

    var functionName = CustomCommandScriptBuilder.BuildFunctionName(command.Name);
    var script = BuildInvocationScript(command, functionName, bindingValues.Length);
    var runner = new ScriptRunner(environment);

    try
    {
      await runner.RunScriptAsync(script, token);
    }
    catch(OperationCanceledException) when(token.IsCancellationRequested)
    {
      throw;
    }
    catch(Exception exception)
    {
      return FailedResult(exception.Message);
    }

    return environment.TryGetUserValue(ResultVariableName, out var result)
      ? new CommandResult { Success = true, Result = result }
      : FailedResult($"Custom command '{command.Name}' completed without producing a result.");
  }

  private static AresValue[] ResolveBindings(
    CustomCommandVersion command,
    IReadOnlyList<Parameter> argumentBindings,
    out string? error)
  {
    var bindingsByName = new Dictionary<string, Parameter>(StringComparer.Ordinal);
    foreach(var binding in argumentBindings)
    {
      var name = binding.Metadata?.Name;
      if(string.IsNullOrWhiteSpace(name))
      {
        error = "Custom command argument bindings must have parameter metadata names.";
        return [];
      }

      if(!bindingsByName.TryAdd(name, binding))
      {
        error = $"Custom command has multiple bindings for parameter '{name}'.";
        return [];
      }
    }

    var values = new AresValue[command.InputParameters.Count];
    for(var i = 0; i < command.InputParameters.Count; i++)
    {
      var input = command.InputParameters[i];
      if(!bindingsByName.Remove(input.Name, out var binding))
      {
        error = $"Custom command '{command.Name}' requires an argument named '{input.Name}'.";
        return [];
      }

      var value = binding.GetValue();
      if(value is null)
      {
        error = $"Custom command argument '{input.Name}' does not have a resolved value.";
        return [];
      }

      values[i] = value.Clone();
    }

    if(bindingsByName.Count > 0)
    {
      error = $"Custom command '{command.Name}' received an unknown argument named '{bindingsByName.Keys.First()}'.";
      return [];
    }

    error = null;
    return values;
  }

  private static string BuildInvocationScript(CustomCommandVersion command, string functionName, int argumentCount)
  {
    var parameters = command.InputParameters.Select(parameter => new AresScriptParameter(
      parameter.Name,
      parameter.Schema ?? new AresValueSchema()));
    var wrappedScript = CustomCommandScriptBuilder.BuildWrappedScript(
      command.Name,
      parameters,
      command.OutputSchema,
      command.ScriptBody);
    var arguments = string.Join(", ", Enumerable.Range(0, argumentCount).Select(index => $"{ArgumentVariablePrefix}{index}"));

    return $"{wrappedScript}\n\n{ResultVariableName} = {functionName}({arguments})";
  }

  private static CommandResult FailedResult(string error) => new()
  {
    Success = false,
    Error = error
  };
}
