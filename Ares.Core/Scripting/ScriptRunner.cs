using System.Reactive.Linq;
using System.Reactive.Subjects;
using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript;
using AresScript.Generated;
using AresScript.Interpreters;

namespace Ares.Core.Scripting;

public class ScriptRunner
{
  private readonly ISubject<string> _outputSubject = new Subject<string>();
  private readonly AresScriptEnvironment _initialEnvironment;

  public ScriptRunner(AresScriptEnvironment? initialEnvironment = null)
  {
    ScriptOutput = _outputSubject.AsObservable();
    Print = new AresSystemFunction("print", "print", (args, _) =>
    {
      var stringy = args.Select(v => v.Stringify());
      foreach(var s in stringy)
      {
        _outputSubject.OnNext(s);
      }

      return Task.FromResult(AresValueHelper.CreateUnit());
    },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
    "",
    "Prints the given value/s of any ARES type to output.");
    _initialEnvironment = initialEnvironment ?? new AresScriptEnvironment();
  }

  public async Task RunScriptAsync(string script, CancellationToken cancellationToken = default)
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
    var visitor = new AresBaseInterpreter(env, cancellationToken);

    await visitor.Visit(programCtx);
  }

  private AresSystemFunction Print { get; }

  public IObservable<string> ScriptOutput { get; }
}
