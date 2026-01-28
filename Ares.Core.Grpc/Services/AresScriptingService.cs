using System.Threading.Tasks;
using Ares.Services;
using Grpc.Core;
using System;
using System.Threading.Channels;
using Ares.Core.Scripting;
using Microsoft.Extensions.Logging;
using Google.Protobuf.WellKnownTypes;
using AresScript;
using AresScript.ScriptAnalysis;
using Ares.Datamodel.Scripting;

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
    var items = await AresScriptAnalysis.BuildCompletionsAsync(
      environment,
      request.Script,
      request.CursorLine,
      request.CursorColumn);
    var response = new CompletionResponse();
    response.Items.AddRange(items);
    return response;
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
