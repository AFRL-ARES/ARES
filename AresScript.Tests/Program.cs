using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;
using AresScript.Generated;
using AresScript.Interpreters;
using AresScript.ScriptAnalysis;
using NUnit.Framework;
using System.Diagnostics;

namespace AresScript.Tests;

[TestFixture]
public class InterpreterTests
{
  private sealed class ThrowingLexerErrorListener : IAntlrErrorListener<int>
  {
    public void SyntaxError(
      TextWriter output,
      IRecognizer recognizer,
      int offendingSymbol,
      int line,
      int column,
      string msg,
      RecognitionException e)
    {
      throw new AresInterpreterException($"Syntax error: {msg}", line, column);
    }
  }

  private sealed class ThrowingParserErrorListener : BaseErrorListener
  {
    public override void SyntaxError(
      TextWriter output,
      IRecognizer recognizer,
      IToken offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e)
    {
      throw new AresInterpreterException($"Syntax error at {line}:{charPositionInLine} - {msg}");
    }
  }

  private static Task RunScriptAsync(
    string script,
    CancellationToken cancellationToken = default,
    Action<AresFunctionInvocation>? invocationObserver = null,
    Action<AresFunctionExecutionEvent>? executionEventObserver = null)
  {
    return RunScriptAsync(script, new ScriptExecutionControlToken(cancellationToken), invocationObserver, executionEventObserver);
  }

  private static async Task RunScriptAsync(
    string script,
    ScriptExecutionControlToken executionControlToken,
    Action<AresFunctionInvocation>? invocationObserver = null,
    Action<AresFunctionExecutionEvent>? executionEventObserver = null)
  {
    var stream = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(stream);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(new ThrowingLexerErrorListener());
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(new ThrowingParserErrorListener());
    var programCtx = parser.program();
    var env = new AresScriptEnvironment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);
    var visitor = new AresBaseInterpreter(env, executionControlToken, invocationObserver, executionEventObserver);

    await visitor.Visit(programCtx);
  }

  private static async Task ValidateScriptAsync(string script)
  {
    var stream = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(stream);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(new ThrowingLexerErrorListener());
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(new ThrowingParserErrorListener());
    var programCtx = parser.program();
    var env = new AresScriptEnvironment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);
    var visitor = new AresValidationInterpreter(env);

    await visitor.Visit(programCtx);
  }

  private static async Task<AresFunctionInvocation[]> ValidateAndCollectInvocationsAsync(string script)
  {
    var env = new AresScriptEnvironment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);

    var (invocations, diagnostics) = await AresScriptAnalysis.ValidateAndCollectInvocationsAsync(script, env);
    Assert.That(diagnostics, Is.Empty);
    return invocations;
  }

  private static async Task<AresFunctionInvocation[]> RunAndCollectRuntimeInvocationsAsync(string script)
  {
    var invocations = new List<AresFunctionInvocation>();
    await RunScriptAsync(script, invocationObserver: invocations.Add);
    return invocations.ToArray();
  }

  private static async Task<AresFunctionExecutionEvent[]> RunAndCollectRuntimeExecutionEventsAsync(string script)
  {
    var events = new List<AresFunctionExecutionEvent>();
    await RunScriptAsync(script, executionEventObserver: events.Add);
    return events.ToArray();
  }

  private static async Task<ScriptFunctionInvocation[]> BuildScriptSummaryAsync(
    string script,
    bool includeUserFunctions = false,
    bool includeLambdas = false)
  {
    var env = new AresScriptEnvironment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);

    var (steps, diagnostics) = await AresScriptAnalysis.BuildScriptSummaryAsync(
      script,
      env,
      includeUserFunctions,
      includeLambdas);
    Assert.That(diagnostics, Is.Empty);
    return steps;
  }

  private static async Task<CompletionItem[]> BuildCompletionsAsync(string script, int line, int column)
  {
    var env = new AresScriptEnvironment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);
    var completions = await AresScriptAnalysis.BuildCompletionsAsync(env, script, line, column);
    return completions.ToArray();
  }

  private static SchemaEntry InferExpressionSchema(
    string expression,
    Action<AresScriptEnvironment>? configureEnvironment = null)
  {
    var stream = new AntlrInputStream(expression);
    var lexer = new AresIndentationLexer(stream);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(new ThrowingLexerErrorListener());
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(new ThrowingParserErrorListener());
    var expressionContext = parser.expression();

    var env = new AresScriptEnvironment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);
    configureEnvironment?.Invoke(env);

    var visitor = new AresTypeInferenceInterpreter(env);
    return visitor.Visit(expressionContext);
  }

  [Test]
  public async Task Assert_Passes_OnTrueCondition()
  {
    var script = """
      assert True
      assert 1 + 2 == 3
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public Task Assert_Fails_OnFalseCondition()
  {
    var script = """
      num1 = 20
      num2 = 40
      assert num1 + num2 == 80
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Assertion failed"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Assert_Fails_OnNonBooleanCondition()
  {
    var script = """
      assert 123
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Assert condition must be boolean"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task KeywordArgs_BindCorrectly()
  {
    var script = """
      def add(a, b):
        return a + b

      assert add(1, 2) == 3
      assert add(a=1, b=2) == 3
      assert add(1, b=2) == 3
      assert add(b=2, a=1) == 3
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task Function_TypeHints_Are_Parsed_For_Parameters_And_Returns()
  {
    var script = """
      def identity(value: Number) -> Number:
        return value

      assert identity(42) == 42
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task Validation_Allows_Function_TypeHints()
  {
    var script = """
      def format_value(value: Number) -> String:
        return "ok"
      """;

    await ValidateScriptAsync(script);
  }

  [Test]
  public Task Runtime_Rejects_Mismatched_Function_Argument_TypeHint()
  {
    var script = """
      def echo_num(value: Number) -> Number:
        return value

      echo_num("oops")
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("argument 'value' type mismatch"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validation_Rejects_Mismatched_Function_Argument_TypeHint()
  {
    var script = """
      def echo_num(value: Number) -> Number:
        return value

      echo_num("oops")
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("argument 'value' type mismatch"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Runtime_Rejects_Mismatched_Function_Return_TypeHint()
  {
    var script = """
      def bad_return() -> Number:
        return "oops"

      bad_return()
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("return type mismatch"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validation_Rejects_Mismatched_Function_Return_TypeHint()
  {
    var script = """
      def bad_return() -> Number:
        return "oops"
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("return type mismatch"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Completions_Include_DataTypes_In_Function_TypeHint_Context()
  {
    var script = "def typed(value: ";
    var completions = await BuildCompletionsAsync(script, 1, script.Length + 1);
    var labels = completions.Select(item => item.Label).ToHashSet(StringComparer.Ordinal);
    Assert.That(labels, Does.Contain("Number"));
    Assert.That(labels, Does.Contain("String"));
    Assert.That(completions.Any(item => item.Kind == SymbolKind.Type), Is.True);
  }

  [Test]
  public async Task Completions_Include_DataTypes_In_Function_Return_TypeHint_Context()
  {
    var script = "def typed(value: Number) -> ";
    var completions = await BuildCompletionsAsync(script, 1, script.Length + 1);
    var labels = completions.Select(item => item.Label).ToHashSet(StringComparer.Ordinal);
    Assert.That(labels, Does.Contain("Number"));
    Assert.That(labels, Does.Contain("String"));
    Assert.That(completions.Any(item => item.Kind == SymbolKind.Type), Is.True);
  }

  [Test]
  public async Task Completions_DoNot_Include_DataTypes_In_Function_Body_Context()
  {
    var script = """
      def typed(value: Number) -> Number:
        val = 
      """;
    var completions = await BuildCompletionsAsync(script, 2, 9);
    Assert.That(completions.Any(item => item.Kind == SymbolKind.Type), Is.False);
  }

  [Test]
  public Task KeywordArgs_RejectPositionalAfterKeyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(a=1, 2)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Positional argument follows keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectUnexpectedKeyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(c=1)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("unexpected keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectMissingRequiredArgument()
  {
    var script = """
      def add(a, b):
        return a + b

      add(1)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("missing required argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectDuplicateKeyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(a=1, a=2)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Duplicate keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectOnRuntimeFunctions()
  {
    var script = "print(value=1)";

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("does not support keyword arguments"));
    return Task.CompletedTask;
  }

  [Test]
  public Task SyntaxErrors_AreThrown()
  {
    var script = """
      if True
        print("oops")
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Syntax error"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Validation_AllowsMemberAccess_OnUnknownFunctionParameter()
  {
    var script = """
      def meme(bepis):
        bepis.GetTemperature()
      """;

    await ValidateScriptAsync(script);
  }

  [Test]
  public async Task Validation_AssignsVariable_FromUserFunctionCall()
  {
    var script = """
      def main():
        return 1

      bepis = main()
      assert bepis == 1
      """;

    await ValidateScriptAsync(script);
  }

  [Test]
  public void TypeInference_Resolves_Parenthesized_System_Function_Call_Output_Schema()
  {
    var schema = InferExpressionSchema("(range)(3)");
    Assert.That(schema.Type, Is.EqualTo(AresDataType.NumberArray));
  }

  [Test]
  public void TypeInference_Resolves_Extension_Call_From_Struct_String_Index()
  {
    var schema = InferExpressionSchema(
      "payload[\"nums\"].append(3)",
      env =>
      {
        var payload = AresValueHelper.CreateStruct();
        payload.StructValue.Fields["nums"] = AresValueHelper.CreateNumberArray([1, 2]);
        env.AssignVariable("payload", payload);
      });

    Assert.That(schema.Type, Is.EqualTo(AresDataType.Unit));
  }

  [Test]
  public async Task Range_OneArg_GeneratesCorrectSequence()
  {
    var script = """
      r = range(5)
      assert r[0] == 0
      assert r[1] == 1
      assert r[2] == 2
      assert r[3] == 3
      assert r[4] == 4
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Range_TwoArgs_GeneratesCorrectSequence()
  {
    var script = """
      r = range(2, 5)
      assert r[0] == 2
      assert r[1] == 3
      assert r[2] == 4
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Range_ThreeArgs_GeneratesCorrectSequence()
  {
    var script = """
      r = range(0, 10, 2)
      assert r[0] == 0
      assert r[1] == 2
      assert r[2] == 4
      assert r[3] == 6
      assert r[4] == 8
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Range_NegativeStep_GeneratesCorrectSequence()
  {
    var script = """
      r = range(5, 0, -1)
      assert r[0] == 5
      assert r[1] == 4
      assert r[2] == 3
      assert r[3] == 2
      assert r[4] == 1
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public Task Range_ZeroStep_ThrowsException()
  {
    var script = "range(0, 10, 0)";
    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Range step must not be zero"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task WhileLoop_ExecutesCorrectly()
  {
    var script = """
      i = 0
      sum = 0
      while i < 5:
        sum = sum + i
        i = i + 1
      assert sum == 10
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task ForLoop_ExecutesCorrectly()
  {
    var script = """
      sum = 0
      for i in range(5):
        sum = sum + i
      assert sum == 10
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task ForLoop_Break_StopsExecution()
  {
    var script = """
      sum = 0
      for i in range(10):
        if i == 5:
          break
        sum = sum + i
      assert sum == 10
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task ForLoop_Continue_SkipsIteration()
  {
    var script = """
      sum = 0
      for i in range(5):
        if i == 2:
          continue
        sum = sum + i
      assert sum == 8
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Nested_Return_Works()
  {
    var script = """
      def check(val):
        if val > 10:
          return True
        return False

      assert check(20) == True
      assert check(5) == False
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Struct_Member_Access_And_Assignment()
  {
    var script = """
      s = { "x": 10, "y": 20 }
      assert s.x == 10
      s.x = 30
      assert s.x == 30
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Struct_Member_Assignment_Creates_Missing_Field()
  {
    var script = """
      s = { }
      s.z = 42
      assert s.z == 42
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Validator_Does_Not_Execute_Runtime_Functions()
  {
    var script = """
      print("hi")
      """;

    var original = Console.Out;
    var output = new StringWriter();
    Console.SetOut(output);
    try
    {
      await ValidateScriptAsync(script);
    }
    finally
    {
      Console.SetOut(original);
    }

    Assert.That(output.ToString(), Is.Empty);
  }

  [Test]
  public async Task Validator_Collects_Ordered_System_And_User_Function_Invocations()
  {
    var script = """
      def id(v):
        return v

      values = range(3)
      id(values)
      print(values)
      """;

    var invocations = await ValidateAndCollectInvocationsAsync(script);
    Assert.That(invocations.Select(i => i.FunctionId), Is.EqualTo(["range", "id", "print"]));
    Assert.That(invocations.Select(i => i.Kind), Is.EqualTo(
    [
      AresFunctionInvocationKind.System,
      AresFunctionInvocationKind.User,
      AresFunctionInvocationKind.System
    ]));
  }

  [Test]
  public async Task Validator_Collects_Extension_Function_Invocations()
  {
    var script = """
      items = []
      items.append(123)
      """;

    var invocations = await ValidateAndCollectInvocationsAsync(script);
    Assert.That(invocations.Length, Is.EqualTo(1));
    Assert.That(invocations[0].FunctionId, Is.EqualTo("list::append"));
    Assert.That(invocations[0].FunctionName, Is.EqualTo("append"));
    Assert.That(invocations[0].Kind, Is.EqualTo(AresFunctionInvocationKind.Extension));
    Assert.That(invocations[0].Expression, Is.EqualTo("items.append(123)"));
  }

  [Test]
  public async Task Validator_Collects_NumberArray_Extension_Function_Invocations()
  {
    var script = """
      numbers = [1, 2]
      numbers.append(3)
      """;

    var invocations = await ValidateAndCollectInvocationsAsync(script);
    Assert.That(invocations.Length, Is.EqualTo(1));
    Assert.That(invocations[0].FunctionId, Is.EqualTo("number_array::append"));
    Assert.That(invocations[0].FunctionName, Is.EqualTo("append"));
    Assert.That(invocations[0].Kind, Is.EqualTo(AresFunctionInvocationKind.Extension));
    Assert.That(invocations[0].Expression, Is.EqualTo("numbers.append(3)"));
  }

  [Test]
  public async Task Runtime_Collects_Ordered_System_User_And_Extension_Function_Invocations()
  {
    var script = """
      def id(v):
        return v

      values = range(3)
      items = []
      items.append(values)
      id(values)
      print(values)
      """;

    var invocations = await RunAndCollectRuntimeInvocationsAsync(script);
    Assert.That(invocations.Select(i => i.FunctionId), Is.EqualTo(["range", "list::append", "id", "print"]));
    Assert.That(invocations.Select(i => i.Kind), Is.EqualTo(
    [
      AresFunctionInvocationKind.System,
      AresFunctionInvocationKind.Extension,
      AresFunctionInvocationKind.User,
      AresFunctionInvocationKind.System
    ]));
    Assert.That(invocations.Select(i => i.Expression), Is.EqualTo(
    [
      "range(3)",
      "items.append(values)",
      "id(values)",
      "print(values)"
    ]));
  }

  [Test]
  public async Task Runtime_Collects_Lambda_Function_Invocations()
  {
    var script = """
      inc = x => x + 1
      inc(2)
      """;

    var invocations = await RunAndCollectRuntimeInvocationsAsync(script);
    Assert.That(invocations.Length, Is.EqualTo(1));
    Assert.That(invocations[0].Kind, Is.EqualTo(AresFunctionInvocationKind.Lambda));
    Assert.That(invocations[0].FunctionId, Does.StartWith("lambda::"));
    Assert.That(invocations[0].Expression, Is.EqualTo("inc(2)"));
  }

  [Test]
  public async Task Runtime_Function_Execution_Events_Include_Started_And_Completed()
  {
    var script = """
      values = range(3)
      """;

    var events = await RunAndCollectRuntimeExecutionEventsAsync(script);
    Assert.That(events.Select(e => e.Kind), Is.EqualTo(new[]
    {
      AresFunctionExecutionEventKind.Started,
      AresFunctionExecutionEventKind.Completed
    }));
    Assert.That(events[0].Invocation.FunctionId, Is.EqualTo("range"));
    Assert.That(events[1].CallId, Is.EqualTo(events[0].CallId));
    Assert.That(events[1].Result, Is.Not.Null);
  }

  [Test]
  public Task Runtime_Function_Execution_Events_Include_Failed()
  {
    var script = """
      nums = [1]
      nums.append("oops")
      """;

    var events = new List<AresFunctionExecutionEvent>();
    Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script, executionEventObserver: events.Add));
    Assert.That(events.Select(e => e.Kind), Is.EqualTo(new[]
    {
      AresFunctionExecutionEventKind.Started,
      AresFunctionExecutionEventKind.Failed
    }));
    Assert.That(events[1].CallId, Is.EqualTo(events[0].CallId));
    Assert.That(events[1].Error, Is.Not.Empty);
    return Task.CompletedTask;
  }

  [Test]
  public async Task Summary_Defaults_To_System_And_Extension_Functions()
  {
    var script = """
      def identity(v):
        return v

      values = range(3)
      items = []
      items.append(values)
      identity(values)
      print(values)
      """;

    var steps = await BuildScriptSummaryAsync(script);
    Assert.That(steps.Select(s => s.FunctionId), Is.EqualTo(["range", "list::append", "print"]));
    Assert.That(steps.Select(s => s.Order), Is.EqualTo([1, 2, 3]));
  }

  [Test]
  public async Task Summary_Can_Include_User_Functions()
  {
    var script = """
      def identity(v):
        return v

      values = range(3)
      identity(values)
      print(values)
      """;

    var steps = await BuildScriptSummaryAsync(script, includeUserFunctions: true);
    Assert.That(steps.Select(s => s.FunctionId), Is.EqualTo(["range", "identity", "print"]));
  }

  [Test]
  public async Task Summary_Does_Not_Prepend_Function_Body_Invocations_From_Declarations()
  {
    var script = """
      def main():
        return True

      def meme():
        sleep(400)
        return "meme"

      sleep(1000)
      main()
      sleep(1000)
      print(meme())
      """;

    var steps = await BuildScriptSummaryAsync(script, includeUserFunctions: true);
    Assert.That(steps.Select(s => s.FunctionId), Is.EqualTo(["sleep", "main", "sleep", "meme", "print"]));
  }

  [Test]
  public Task Cancellation_Stops_Interpreter()
  {
    var script = "assert True";
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var ex = Assert.ThrowsAsync<OperationCanceledException>(() => RunScriptAsync(script, cts.Token));
    Assert.That(ex, Is.Not.Null);
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Runtime_Keyword_Args()
  {
    var script = "print(value=1)";

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("does not support keyword arguments"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Positional_After_Keyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(a=1, 2)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Positional argument follows keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Missing_Function_At_Top_Level()
  {
    var script = "missing()";

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Unknown identifier 'missing'"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Index_Assignment_Works_For_Number_Array()
  {
    var script = """
      nums = [1, 2, 3]
      nums[0] = 9
      assert nums[0] == 9
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Index_Assignment_Works_For_String_Array()
  {
    var script = """
      words = ["a", "b"]
      words[1] = "c"
      assert words[1] == "c"
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Index_Assignment_Works_For_List()
  {
    var script = """
      items = [1, "a"]
      items[0] = 2
      items[1] = "b"
      assert items[0] == 2
      assert items[1] == "b"
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Numeric_Separators_Work_For_Int_And_Float()
  {
    var script = """
      big = 1_000_000
      assert big == 1000000

      piish = 1_234.5_6
      assert piish == 1234.56
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task List_Append_Works()
  {
    var script = """
      items = []
      items.append(123)
      assert items[0] == 123
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task Number_Array_Append_Works()
  {
    var script = """
      nums = [1, 2]
      nums.append(3)
      assert nums[2] == 3
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task String_Array_Append_Works()
  {
    var script = """
      words = ["a", "b"]
      words.append("c")
      assert words[2] == "c"
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public Task Validator_Rejects_Number_Array_Append_With_Wrong_Type()
  {
    var script = """
      nums = [1, 2]
      nums.append("oops")
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("type mismatch"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Lambda_Expression_Works()
  {
    var script = """
      inc = x => x + 1
      assert inc(2) == 3
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task Lambda_Closure_Captures_By_Value()
  {
    var script = """
      seed = 5
      addSeed = x => x + seed
      seed = 20
      assert addSeed(2) == 7
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public Task Validator_Rejects_Too_Many_Lambda_Args()
  {
    var script = """
      inc = x => x + 1
      inc(1, 2)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("expected 1 arguments but got 2"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Too_Many_Extension_Args()
  {
    var script = """
      items = []
      items.append(1, 2)
      """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("expected at most 1 arguments"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Empty_Array_Literal_Does_Not_Throw()
  {
    var script = """
      empty = []
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public Task Empty_Array_Index_Access_Throws()
  {
    var script = """
                 empty_arr = []
                 test = empty_arr[1]
                 """;
    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Index was out of range."));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Recursion_Works()
  {
    var script = """
      def fib(n):
        if n <= 1:
          return n
        return fib(n - 1) + fib(n - 2)

      assert fib(10) == 55
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Top_Level_Loop_Works()
  {
    var script = """
                 total = 0
                 for i in range(10):
                   total = total + i
                 assert total == 45
                 """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task Inner_Scope_Shadows_Outer_Variable()
  {
    var script = """
      x = 1
      def inner():
        x = 2
        return x
      assert inner() == 2
      assert x == 1
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Inner_Scope_Reassignment_Does_Not_Update_Outer()
  {
    var script = """
      count = 1
      def inner():
        count = 2
      inner()
      assert count == 1
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Inner_Scope_Member_Assignment_Updates_Outer_Struct()
  {
    var script = """
      s = { "x": 1 }
      def inner():
        s.x = 2
      inner()
      assert s.x == 2
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Inner_Scope_Index_Assignment_Updates_Outer_Collection()
  {
    var script = """
      items = [1, 2, 3]
      def inner():
        items[1] = 9
      inner()
      assert items[1] == 9
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Block_At_Eof_Parses_Without_Trailing_Newline()
  {
    var script = "while False:\n  assert True";
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Sleep_Test()
  {
    var script = "sleep(1000)";
    var stopwatch = Stopwatch.StartNew();
    await RunScriptAsync(script);
    stopwatch.Stop();
    Assert.That(stopwatch.ElapsedMilliseconds, Is.AtLeast(990));
  }

  [Test]
  [CancelAfter(5000)]
  public async Task Sleep_Cancel(CancellationToken testToken)
  {
    var script = "sleep(2000)";
    using var cts = new CancellationTokenSource();
    var executionTask = RunScriptAsync(script, cts.Token);
    await Task.Delay(TimeSpan.FromMilliseconds(500), testToken);
    await cts.CancelAsync();
    Assert.ThrowsAsync<TaskCanceledException>(() => executionTask);
  }

  [Test]
  [CancelAfter(5000)]
  public async Task Pause_Blocks_Execution_Until_Resume(CancellationToken testToken)
  {
    var script = "sleep(200)";
    using var control = new ScriptExecutionControlTokenSource(testToken);
    control.Pause();
    var executionTask = RunScriptAsync(script, control.Token);
    await Task.Delay(TimeSpan.FromMilliseconds(250), testToken);
    Assert.That(executionTask.IsCompleted, Is.False);

    control.Resume();
    await executionTask.WaitAsync(testToken);
  }

  [Test]
  public Task Too_Much_Recursion()
  {
    var script = """
                 def recurse(num):
                   if num <= 0:
                     return
                     
                   print(num)
                   recurse(num - 1)

                 recurse(101)
                 """;

    var ex = Assert.ThrowsAsync<AresInterpreterException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Maximum function call depth reached."));
    return Task.CompletedTask;
  }

  [Test]
  [Retry(5)]
  public async Task Parallel_Task()
  {
    var script = """
                 parallel:
                   sleep(200)
                   sleep(200)
                   sleep(200)
                   sleep(200)
                   sleep(200)
                 """;
    var stopwatch = Stopwatch.StartNew();
    await RunScriptAsync(script);
    stopwatch.Stop();

    // All sleeps together make up 1 second
    // parallelized they should take up a total of 200 + execution overhead
    Assert.That(stopwatch.ElapsedMilliseconds, Is.InRange(200, 400));
  }
}
