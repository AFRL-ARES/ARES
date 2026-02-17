using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript;
using AresScript.Generated;
using AresScript.Interpreters;
using AresScript.ScriptAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Ares.Core.Scripting;

public class ScriptRunner
{
  private readonly Subject<string> _outputSubject = new();
  private readonly ISubject<AresFunctionInvocation> _invocationSubject = Subject.Synchronize(new Subject<AresFunctionInvocation>());
  private readonly ISubject<ScriptExecutionEvent> _eventSubject = Subject.Synchronize(new Subject<ScriptExecutionEvent>());
  private readonly AresScriptEnvironment _initialEnvironment;
  private readonly bool _captureExecutionEventsWithoutSubscribers;
  private int _scriptEventSubscriberCount;
  private long _eventSequence;

  public ScriptRunner(
    AresScriptEnvironment? initialEnvironment = null,
    bool captureExecutionEventsWithoutSubscribers = false)
  {
    _captureExecutionEventsWithoutSubscribers = captureExecutionEventsWithoutSubscribers;
    ScriptOutput = _outputSubject.AsObservable();
    ScriptInvocations = _invocationSubject.AsObservable();
    ScriptEvents = Observable.Create<ScriptExecutionEvent>(observer =>
    {
      Interlocked.Increment(ref _scriptEventSubscriberCount);
      var subscription = _eventSubject.Subscribe(observer);
      return Disposable.Create(() =>
      {
        subscription.Dispose();
        Interlocked.Decrement(ref _scriptEventSubscriberCount);
      });
    });
    Print = new AresSystemFunction("print", "print", (args, _) =>
    {
      var stringy = args.Select(v => v.Stringify());
      foreach(var s in stringy)
      {
        _outputSubject.OnNext(s);
        if(ShouldEmitScriptEvents())
        {
          PublishEvent(sequence => new ScriptConsoleOutputEvent(sequence, s));
        }
      }

      return Task.FromResult(AresValueHelper.CreateUnit());
    },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
    "",
    "Prints the given value/s of any ARES type to output.");
    _initialEnvironment = initialEnvironment ?? new AresScriptEnvironment();
  }

  public Task RunScriptAsync(string script, CancellationToken cancellationToken = default)
  {
    return RunScriptAsync(script, new ScriptExecutionControlToken(cancellationToken));
  }

  public async Task RunScriptAsync(string script, ScriptExecutionControlToken executionControlToken)
  {
    var stream = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(stream);
    //lexer.RemoveErrorListeners();
    //lexer.AddErrorListener(new ThrowingLexerErrorListener());
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    //parser.RemoveErrorListeners();
    //parser.AddErrorListener(new ThrowingParserErrorListener());
    var programCtx = parser.program();
    var env = _initialEnvironment;
    env.EnterSystemScope("SandboxRunner");
    env.AssignSystemFunctions(Print);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);
    var emitScriptEvents = ShouldEmitScriptEvents();
    var visitor = new AresBaseInterpreter(
      env,
      executionControlToken,
      invocation => _invocationSubject.OnNext(invocation),
      emitScriptEvents
        ? executionEvent =>
        {
          switch(executionEvent.Kind)
          {
            case AresFunctionExecutionEventKind.Started:
              PublishEvent(sequence => new ScriptFunctionStartedEvent(
                sequence,
                executionEvent.CallId,
                executionEvent.ParentCallId,
                AresFunctionInvocationMapper.ToScriptFunctionInvocation(executionEvent.Invocation, 0)));
              break;
            case AresFunctionExecutionEventKind.Completed:
              PublishEvent(sequence => new ScriptFunctionCompletedEvent(
                sequence,
                executionEvent.CallId,
                executionEvent.Result ?? AresValueHelper.CreateUnit()));
              break;
            case AresFunctionExecutionEventKind.Failed:
              PublishEvent(sequence => new ScriptFunctionFailedEvent(
                sequence,
                executionEvent.CallId,
                executionEvent.Error ?? string.Empty));
              break;
          }
        }
        : null);

    if(emitScriptEvents)
    {
      PublishEvent(sequence => new ScriptExecutionStartedEvent(sequence));
    }
    try
    {
      await visitor.Visit(programCtx);
      if(emitScriptEvents)
      {
        PublishEvent(sequence => new ScriptExecutionCompletedEvent(sequence));
      }
    }
    catch(Exception e)
    {
      if(emitScriptEvents)
      {
        PublishEvent(sequence => new ScriptExecutionFailedEvent(sequence, e.ToString()));
      }
      throw;
    }
  }

  private AresSystemFunction Print { get; }

  public IObservable<string> ScriptOutput { get; }
  public IObservable<AresFunctionInvocation> ScriptInvocations { get; }
  public IObservable<ScriptExecutionEvent> ScriptEvents { get; }

  private void PublishEvent(Func<long, ScriptExecutionEvent> eventFactory)
  {
    var sequence = Interlocked.Increment(ref _eventSequence);
    _eventSubject.OnNext(eventFactory(sequence));
  }

  private bool ShouldEmitScriptEvents()
  {
    return _captureExecutionEventsWithoutSubscribers || Volatile.Read(ref _scriptEventSubscriberCount) > 0;
  }
}
