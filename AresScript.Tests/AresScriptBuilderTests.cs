using Antlr4.Runtime;
using Ares.Datamodel;
using AresScript.Generated;
using AresScript.ScriptBuilding;
using AresScript.Symbols;
using NUnit.Framework;

namespace AresScript.Tests;

[TestFixture]
public class AresScriptBuilderTests
{
  [Test]
  public void Build_ProducesParseableScript_WithNestedFeatures()
  {
    var builder = new AresScriptBuilder();
    builder
      .AddAssignment("numbers", "[1, 2, 3]")
      .AddFunction("sum_positive", body =>
      {
        body
          .AddAssignment("sum", "0")
          .AddFor("value", "values", loop =>
          {
            loop.AddIf("value > 0", positive => positive.AddAssignment("sum", "sum + value"));
          })
          .AddReturn("sum");
      }, "values")
      .AddIf(
        "len(numbers) > 0",
        thenBranch => thenBranch.AddExpression("print(sum_positive(numbers))"),
        branches => branches.AddElse(elseBranch => elseBranch.AddExpression("print(0)")))
      .AddParallel(parallel => parallel
        .AddExpression("print(\"first\")")
        .AddExpression("print(\"second\")"));

    var script = builder.Build();

    Assert.That(script, Does.Contain("def sum_positive(values):"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void AddReturn_OutsideFunction_Throws()
  {
    var builder = new AresScriptBuilder();
    var ex = Assert.Throws<InvalidOperationException>(() => builder.AddReturn("1"));
    Assert.That(ex?.Message, Does.Contain("function bodies"));
  }

  [Test]
  public void AddBreak_OutsideLoop_Throws()
  {
    var builder = new AresScriptBuilder();
    var ex = Assert.Throws<InvalidOperationException>(() => builder.AddBreak());
    Assert.That(ex?.Message, Does.Contain("loop bodies"));
  }

  [Test]
  public void FromScript_CanEditExistingFunction_AndBuildValidScript()
  {
    var existingScript = """
      def main(value):
        total = value + 1
        return total

      answer = main(1)
      """;

    var builder = AresScriptBuilder.FromScript(existingScript);
    var edited = builder.EditFunction("main", body =>
    {
      body.AddAssignment("total", "total + 10");
    });

    var script = builder.Build();

    Assert.That(edited, Is.True);
    Assert.That(script, Does.Contain("total = total + 10"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void FromScript_CanRemoveFunction()
  {
    var existingScript = """
      def helper():
        return 1

      value = 10
      """;

    var builder = AresScriptBuilder.FromScript(existingScript);
    var removed = builder.RemoveFunction("helper");
    var script = builder.Build();

    Assert.That(removed, Is.True);
    Assert.That(script, Does.Not.Contain("def helper():"));
    Assert.That(script, Does.Contain("value = 10"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void ReplaceFunction_WritesParameterTypeHints()
  {
    var builder = new AresScriptBuilder();
    builder.ReplaceFunction(
      "typed_fn",
      body => body.AddReturn("value"),
      new AresScriptParameter("value", AresDataType.String),
      new AresScriptParameter("count", AresDataType.Number));

    var script = builder.Build();

    Assert.That(script, Does.Contain("def typed_fn(value: String, count: Number):"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void EditFunction_PreservesImportedParameterTypeHints()
  {
    var existingScript = """
      def typed_fn(value: String, count: Number):
        return value
      """;

    var builder = AresScriptBuilder.FromScript(existingScript);
    var edited = builder.EditFunction("typed_fn", body => body.AddAssignment("count", "count + 1"));
    var script = builder.Build();

    Assert.That(edited, Is.True);
    Assert.That(script, Does.Contain("def typed_fn(value: String, count: Number):"));
    Assert.That(script, Does.Contain("count = count + 1"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  private static void Parse(string script)
  {
    var input = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(input);
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    parser.program();

    Assert.That(parser.NumberOfSyntaxErrors, Is.EqualTo(0));
  }
}
