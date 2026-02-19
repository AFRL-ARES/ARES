using Ares.Core.Scripting;
using Ares.Services;
using AresScript.Interpreters;
using AresScript.ScriptAnalysis;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;
using System.Threading.Channels;
using System.Threading.Tasks;
using CoreScriptExecutionEvent = Ares.Core.Scripting.ScriptExecutionEvent;
using GrpcScriptExecutionEvent = Ares.Services.ScriptExecutionEvent;

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
  public override async Task ExecuteScript(ScriptExecutionRequest request, IServerStreamWriter<GrpcScriptExecutionEvent> responseStream, ServerCallContext context)
  {
    var channel = Channel.CreateBounded<GrpcScriptExecutionEvent>(new BoundedChannelOptions(100));
    var env = _environmentBuilder.Build();
    var runner = new ScriptRunner(env);
    var subscriptions = new CompositeDisposable
    {
      runner.ScriptEvents.Subscribe(executionEvent =>
        {
          var grpcEvent = ToGrpcScriptExecutionEvent(executionEvent);
          if(!channel.Writer.TryWrite(grpcEvent))
          {
            _logger.LogWarning("Dropped script event because channel is full. {Sequence}", grpcEvent.Sequence);
          }
        })
    };

    async Task ReadOutputAsync()
    {
      try
      {
        await foreach(var val in channel.Reader.ReadAllAsync(context.CancellationToken))
        {
          await responseStream.WriteAsync(val);
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
      channel.Writer.TryComplete(e);
      _logger.LogError("Script runner failed: {Exception}", e);
      throw;
    }
    finally
    {
      subscriptions.Dispose();
    }

    await readTask;
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

  public override async Task<ScriptSummaryResponse> GetScriptSummary(ScriptSummaryRequest request, ServerCallContext context)
  {
    var environment = _environmentBuilder.Build();
    var (steps, diagnostics) = await AresScriptAnalysis.BuildScriptSummaryAsync(
      request.Script,
      environment,
      request.IncludeUserFunctions,
      request.IncludeLambdas,
      AresValidationInterpreter.ValidationMode.Strict);

    var response = new ScriptSummaryResponse();
    response.Steps.AddRange(steps);
    response.Diagnostics.AddRange(diagnostics);
    return response;
  }

  private static GrpcScriptExecutionEvent ToGrpcScriptExecutionEvent(CoreScriptExecutionEvent executionEvent)
  {
    return executionEvent switch
    {
      ScriptExecutionStartedEvent started => new GrpcScriptExecutionEvent
      {
        Sequence = started.Sequence,
        ExecutionStarted = new ExecutionStarted()
      },
      ScriptExecutionCompletedEvent completed => new GrpcScriptExecutionEvent
      {
        Sequence = completed.Sequence,
        ExecutionCompleted = new ExecutionCompleted()
      },
      ScriptExecutionFailedEvent failed => new GrpcScriptExecutionEvent
      {
        Sequence = failed.Sequence,
        ExecutionFailed = new ExecutionFailed { Error = failed.Error }
      },
      ScriptConsoleOutputEvent output => new GrpcScriptExecutionEvent
      {
        Sequence = output.Sequence,
        ConsoleOutput = new ConsoleOutput { Output = output.Output }
      },
      ScriptFunctionStartedEvent started => new GrpcScriptExecutionEvent
      {
        Sequence = started.Sequence,
        FunctionStarted = new FunctionStarted
        {
          CallId = started.CallId,
          ParentCallId = started.ParentCallId,
          Invocation = started.Invocation
        }
      },
      ScriptFunctionCompletedEvent completed => new GrpcScriptExecutionEvent
      {
        Sequence = completed.Sequence,
        FunctionCompleted = new FunctionCompleted
        {
          CallId = completed.CallId,
          Result = completed.Result
        }
      },
      ScriptFunctionFailedEvent failed => new GrpcScriptExecutionEvent
      {
        Sequence = failed.Sequence,
        FunctionFailed = new FunctionFailed
        {
          CallId = failed.CallId,
          Error = failed.Error
        }
      },
      _ => throw new ArgumentOutOfRangeException(nameof(executionEvent), executionEvent, "Unhandled script execution event type")
    };
  }
}
