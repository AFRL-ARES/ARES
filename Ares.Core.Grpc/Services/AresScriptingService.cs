using System.Threading.Tasks;
using Ares.Services;
using Grpc.Core;
using System;
using System.Threading.Channels;
using Ares.Core.Scripting;
using Microsoft.Extensions.Logging;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using System.Linq;
using AresScript;
using Ares.Datamodel.Scripting;
using Ares.Datamodel;
using System.Text;

namespace Ares.Core.Grpc.Services;

public partial class AresScriptingService : Ares.Services.AresScriptingService.AresScriptingServiceBase
{
  private readonly ILogger<AresScriptingService> _logger;
  private readonly BaseEnvironmentBuilder _environmentBuilder;

  public AresScriptingService(ILogger<AresScriptingService> logger, BaseEnvironmentBuilder environmentBuilder)
  {
    _logger = logger;
    _environmentBuilder = environmentBuilder;

  }
  public override async Task ExecuteScript(ScriptExecutionRequest request, IServerStreamWriter<ScriptExecutionOutput> responseStream, ServerCallContext context)
  {
    var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100));
    var env = _environmentBuilder.Build();
    var runner = new ScriptRunner(env);
    runner.ScriptOutput.Subscribe(output =>
    {
      if(!channel.Writer.TryWrite(output))
      {
        _logger.LogWarning("Dropped script output because channel is full. {Output}", output);
      }
    });
    async Task ReadOutputAsync()
    {
      try
      {
        await foreach(var val in channel.Reader.ReadAllAsync(context.CancellationToken))
        {
          await responseStream.WriteAsync(new ScriptExecutionOutput { Output = val });
        }
      }
      catch(OperationCanceledException) when(context.CancellationToken.IsCancellationRequested)
      {
        _logger.LogInformation("Grpc stream cancelled while sending script output.");
      }
      catch(RpcException e)
      {
        _logger.LogError("RpcException while trying to write to the grpc stream: {Exception}", e);
      }
      catch(Exception e)
      {
        _logger.LogError($"Exception while reading script output. {e}");
      }
    }
    var readTask = ReadOutputAsync();

    try
    {
      await runner.RunScriptAsync(request.Script, context.CancellationToken);
      channel.Writer.TryComplete();
    }
    catch(Exception e)
    {
      channel.Writer.TryWrite($"Run failed: {e}");
      channel.Writer.TryComplete(e);
      _logger.LogError("Script runner failed: {Exception}", e);
      throw;
    }
    await readTask;
  }

  public override Task<AutocompleteCatalogResponse> GetAutocompleteCatalog(Empty request, ServerCallContext context)
  {
    var environment = _environmentBuilder.Build();
    var catalog = AresScriptAnalysis.BuildAutocompleteCatalog(environment);
    var response = new AutocompleteCatalogResponse
    {
      Catalog = catalog
    };
    return Task.FromResult(response);
  }

  public override async Task<CompletionResponse> GetCompletions(CompletionRequest request, ServerCallContext context)
  {
    var environment = _environmentBuilder.Build();
    await AresScriptAnalysis.BuildEnvironmentForCompletions(environment, request.Script);
    var catalog = AresScriptAnalysis.BuildAutocompleteCatalog(environment);
    var systemFunctions = environment.GetAllSystemFunctions();
    var userFunctions = environment.GetAllUserFunctions();
    var userVariables = environment.GetAllUserVariableNames();
    var items = new List<CompletionItem>();

    if(AresScriptAnalysis.TryGetParentIdentifier(request.Script, request.CursorLine, request.CursorColumn, out var parentIdentifier))
    {
      var ns = catalog.Namespaces.FirstOrDefault(n => string.Equals(n.Identifier, parentIdentifier, StringComparison.Ordinal));
      if(ns is not null)
      {
        items.AddRange(ns.Functions.Select(func => new CompletionItem
        {
          Label = func.Name,
          InsertText = BuildSnippet(func.Name, func.InputSchema),
          Detail = func.Description,
          Documentation = func.Description,
          Kind = CompletionItemKind.Function,
          ParentIdentifier = ns.Identifier,
          InputSchema = func.InputSchema,
          OutputSchema = func.OutputSchema
        }));
      }
    }
    else
    {
      items.AddRange(catalog.Namespaces.Select(ns => new CompletionItem
      {
        Label = ns.Identifier,
        InsertText = ns.Identifier,
        Detail = ns.DisplayName,
        Documentation = ns.Description,
        Kind = AresScriptAnalysis.MapNamespaceKindToCompletionKind(ns.Kind),
        ParentIdentifier = string.Empty
      }));

      items.AddRange(systemFunctions
        .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
        .Select(func => new CompletionItem
        {
          Label = func.Name,
          InsertText = BuildSnippet(func.Name, func.InputSchema),
          Detail = func.Description,
          Documentation = func.Description,
          Kind = CompletionItemKind.Function,
          InputSchema = func.InputSchema,
          OutputSchema = func.OutputSchema
        }));

      items.AddRange(userFunctions.Select(func => new CompletionItem
      {
        Label = func.Name,
        InsertText = func.Name,
        Detail = "User function",
        Kind = CompletionItemKind.Function
      }));

      items.AddRange(userVariables.Select(name => new CompletionItem
      {
        Label = name,
        InsertText = name,
        Detail = "User variable",
        Kind = CompletionItemKind.Variable
      }));

      items.AddRange(catalog.Globals.Select(global => new CompletionItem
      {
        Label = global.Name,
        InsertText = global.Name,
        Detail = global.Description,
        Documentation = global.Description,
        Kind = CompletionItemKind.Variable,
        Schema = global.Schema
      }));
    }

    var response = new CompletionResponse();
    response.Items.AddRange(items);
    return response;
  }

  private static string BuildSnippet(string funcName, AresDataSchema schema)
  {
    var builder = new StringBuilder();
    builder.Append(funcName);
    builder.Append('(');
    var requiredFields = schema.Fields.Where(field => !field.Value.Optional).ToList();
    for(var i = 0; i < requiredFields.Count; i++)
    {
      var fieldElement = requiredFields[i];
      builder.Append($"${{{i + 1}:{fieldElement.Key}}}");
      if(i < requiredFields.Count - 1)
      {
        builder.Append(',');
      }
    }
    builder.Append(')');
    return builder.ToString();
  }

  public override async Task<ValidateScriptResponse> ValidateScript(ValidateScriptRequest request, ServerCallContext context)
  {
    var environment = _environmentBuilder.Build();
    var diagnostics = await AresScriptAnalysis.ValidateScriptAsync(
      request.Script,
      environment,
      AresValidationInterpreter.ValidationMode.Strict);

    var response = new ValidateScriptResponse();
    response.Diagnostics.AddRange(diagnostics);
    return response;
  }
}
