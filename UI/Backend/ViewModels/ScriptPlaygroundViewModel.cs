using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Services;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Infrastructure.Monaco.Interops;

namespace UI.Backend.ViewModels;

public partial class ScriptPlaygroundViewModel : ReactiveObject
{
  private readonly AresScriptingService.AresScriptingServiceClient _scriptingClient;
  private CancellationTokenSource _cancellationTokenSource = new();
  private readonly ISubject<string> _scriptOutput = new Subject<string>();

  public ScriptPlaygroundViewModel(
    AresScriptingService.AresScriptingServiceClient scriptingClient,
    MonacoCompletionProvider completionProvider,
    MonacoDiagnosticsProvider diagnosticsProvider,
    MonacoSemanticTokensProvider semanticTokensProvider,
    MonacoHoverProvider hoverProvider)
  {
    _scriptingClient = scriptingClient;
    CompletionProvider = completionProvider;
    DiagnosticsProvider = diagnosticsProvider;
    SemanticTokensProvider = semanticTokensProvider;
    HoverProvider = hoverProvider;
    ScriptOutput = _scriptOutput.AsObservable();
  }

  public async Task StartScript(string script)
  {
    _cancellationTokenSource = new();
    var token = _cancellationTokenSource.Token;
    ScriptRunning = true;
    var something = _scriptingClient.ExecuteScript(new ScriptExecutionRequest { Script = script }, new CallOptions(cancellationToken: token));

    try
    {
      await foreach(var output in something.ResponseStream.ReadAllAsync(token))
      {
        _scriptOutput.OnNext(output.Output);
      }
    }
    catch(RpcException)
    {
    }
    finally
    {
      ScriptRunning = false;
    }
  }

  public async Task StopScript()
  {
    await _cancellationTokenSource.CancelAsync();
  }

  [Reactive]
  public partial bool ScriptRunning { get; private set; }

  public IObservable<string> ScriptOutput { get; }
  public MonacoCompletionProvider CompletionProvider { get; }
  public MonacoDiagnosticsProvider DiagnosticsProvider { get; }
  public MonacoSemanticTokensProvider SemanticTokensProvider { get; }
  public MonacoHoverProvider HoverProvider { get; }
}
