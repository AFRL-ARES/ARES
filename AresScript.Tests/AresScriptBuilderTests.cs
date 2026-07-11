using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Factories;
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
  public void AddOrReplaceFunction_WritesParameterTypeHints()
  {
    var builder = new AresScriptBuilder();
    builder.AddOrReplaceFunction(
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

  [Test]
  public void EditFunction_PreservesImportedSchemaTypeHints()
  {
    var existingScript = """
      def typed_fn(value: { foo: Number, bar: String }) -> { foo: Number, bar: String }:
        return value
      """;

    var builder = AresScriptBuilder.FromScript(existingScript);
    var edited = builder.EditFunction("typed_fn", body => body.AddAssignment("value", "{ foo: 1, bar: \"ok\" }"));
    var script = builder.Build();

    Assert.That(edited, Is.True);
    Assert.That(script, Does.Contain("def typed_fn(value: {foo: Number, bar: String}) -> {foo: Number, bar: String}:"));
    Assert.That(script, Does.Contain("value = { foo: 1, bar: \"ok\" }"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void AddOrReplaceFunction_WritesConstrainedQuantityTypeHints()
  {
    var valueSchema = AresSchemaBuilder.Entry(AresDataType.Quantity)
      .WithQuantityRange(QuantityType.Duration, "s", minScalarValue: 0, maxScalarValue: 30)
      .Build();

    var builder = new AresScriptBuilder();
    builder.AddOrReplaceFunction(
      "typed_fn",
      body => body.AddReturn("value"),
      new AresScriptParameter("value", valueSchema));

    var script = builder.Build();

    Assert.That(script, Does.Contain("def typed_fn(value: Quantity.Duration[unit=\"s\", min=0, max=30]):"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void ReplaceFunction_WritesProgrammaticQuantityReturnTypeHint()
  {
    var valueSchema = AresSchemaBuilder.Entry(AresDataType.Quantity)
      .WithQuantityRange(QuantityType.Duration, "s", minScalarValue: 0, maxScalarValue: 30)
      .Build();

    var existingScript = """
      def typed_fn(value):
        return value
      """;

    var builder = AresScriptBuilder.FromScript(existingScript);
    var replaced = builder.ReplaceFunction(
      "typed_fn",
      body => body.AddReturn("value"),
      valueSchema,
      new AresScriptParameter("value", valueSchema));

    var script = builder.Build();

    Assert.That(replaced, Is.True);
    Assert.That(script, Does.Contain("def typed_fn(value: Quantity.Duration[unit=\"s\", min=0, max=30]) -> Quantity.Duration[unit=\"s\", min=0, max=30]:"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void AddOrReplaceFunction_WritesConstrainedNumberTypeHints()
  {
    var valueSchema = AresSchemaBuilder.Entry(AresDataType.Number)
      .WithNumberRange(minValue: 0, maxValue: 30)
      .Build();

    var builder = new AresScriptBuilder();
    builder.AddOrReplaceFunction(
      "typed_fn",
      body => body.AddReturn("value"),
      new AresScriptParameter("value", valueSchema));

    var script = builder.Build();

    Assert.That(script, Does.Contain("def typed_fn(value: Number[min=0, max=30]):"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void AddOrReplaceFunction_WritesConstrainedFloatAndIntTypeHints()
  {
    var floatSchema = AresSchemaBuilder.Entry(AresDataType.Float)
      .WithNumberRange(minValue: 0, maxValue: 30)
      .Build();
    var intSchema = AresSchemaBuilder.Entry(AresDataType.Int)
      .WithNumberRange(minValue: 1, maxValue: 5)
      .Build();

    var builder = new AresScriptBuilder();
    builder.AddOrReplaceFunction(
      "typed_fn",
      body => body.AddReturn("value"),
      new AresScriptParameter("value", floatSchema),
      new AresScriptParameter("count", intSchema));

    var script = builder.Build();

    Assert.That(script, Does.Contain("def typed_fn(value: Float[min=0, max=30], count: Int[min=1, max=5]):"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void AddFunction_WritesProgrammaticQuantitySchemas()
  {
    var valueSchema = AresSchemaBuilder.Entry(AresDataType.Quantity)
      .WithQuantityRange(QuantityType.Duration, "s", minScalarValue: 0, maxScalarValue: 30)
      .Build();

    var builder = new AresScriptBuilder();
    builder.AddFunction(
      "typed_fn",
      body => body.AddReturn("value"),
      valueSchema,
      new AresScriptParameter("value", valueSchema));

    var script = builder.Build();

    Assert.That(script, Does.Contain("def typed_fn(value: Quantity.Duration[unit=\"s\", min=0, max=30]) -> Quantity.Duration[unit=\"s\", min=0, max=30]:"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void ReplaceFunction_WhenFunctionDoesNotExist_ReturnsFalse()
  {
    var builder = new AresScriptBuilder();

    var replaced = builder.ReplaceFunction(
      "missing_fn",
      body => body.AddReturn("1"),
      new AresScriptParameter("value", AresDataType.Number));

    Assert.That(replaced, Is.False);
    Assert.That(builder.Build(), Is.Empty);
  }

  [Test]
  public void CustomCommandScriptBuilder_BuildFunctionName_SanitizesCommandName()
  {
    var functionName = CustomCommandScriptBuilder.BuildFunctionName("  123 Measure Temperature!! ");

    Assert.That(functionName, Is.EqualTo("custom_command_123_Measure_Temperature"));
  }

  [Test]
  public void CustomCommandScriptBuilder_BuildFunctionSignature_WritesParameterAndReturnTypeHints()
  {
    var outputSchema = AresSchemaBuilder.Entry(AresDataType.Quantity)
      .WithQuantityRange(QuantityType.Temperature, "degC", minScalarValue: 0, maxScalarValue: 100)
      .Build();

    var signature = CustomCommandScriptBuilder.BuildFunctionSignature(
      "Measure Temperature",
      [
        new AresScriptParameter("sample_id", AresDataType.String),
        new AresScriptParameter("timeout_seconds", AresDataType.Number)
      ],
      outputSchema);

    Assert.That(signature, Is.EqualTo("def custom_command_Measure_Temperature(sample_id: String, timeout_seconds: Number) -> Quantity.Temperature[unit=\"degC\", min=0, max=100]"));
  }

  [Test]
  public void CustomCommandScriptBuilder_BuildWrappedScript_WritesNestedSchemaTypeHints()
  {
    var structSchema = AresSchemaBuilder.Entry(AresDataType.Struct).Build();
    structSchema.StructSchema = new AresStructSchema();
    structSchema.StructSchema.Fields["reading"] = AresSchemaBuilder.Entry(AresDataType.Number).Build();
    structSchema.StructSchema.Fields["unit"] = AresSchemaBuilder.Entry(AresDataType.String).Build();

    var listSchema = AresSchemaBuilder.Entry(AresDataType.List).Build();
    listSchema.ListElementSchema = AresSchemaBuilder.Entry(AresDataType.String).Build();

    var script = CustomCommandScriptBuilder.BuildWrappedScript(
      "Summarize Samples",
      [
        new AresScriptParameter("measurement", structSchema),
        new AresScriptParameter("tags", listSchema)
      ],
      structSchema,
      """
      total = measurement.reading

      return measurement
      """);

    Assert.That(script, Does.Contain("def custom_command_Summarize_Samples(measurement: {reading: Number, unit: String}, tags: [String]) -> {reading: Number, unit: String}:"));
    Assert.That(script, Does.Contain("  total = measurement.reading"));
    Assert.That(script, Does.Contain($"{System.Environment.NewLine}  {System.Environment.NewLine}  return measurement"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void CustomCommandScriptBuilder_BuildWrappedScript_PreservesWhitespaceBeforeReturnFallback()
  {
    var script = CustomCommandScriptBuilder.BuildWrappedScript(
      "No Op",
      [],
      AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      "  ");

    Assert.That(
      script,
      Is.EqualTo(
        $"def custom_command_No_Op() -> Unit:{System.Environment.NewLine}    {System.Environment.NewLine}  return"));
    Assert.That(() => Parse(script), Throws.Nothing);
  }

  [Test]
  public void CustomCommandScriptBuilder_BuildWrappedScript_PreservesEveryBodyLine()
  {
    var script = CustomCommandScriptBuilder.BuildWrappedScript(
      "Position Test",
      [],
      AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      "\r\nsleep()\r\n\r\n");

    var newline = System.Environment.NewLine;
    Assert.That(
      script,
      Is.EqualTo(
        $"def custom_command_Position_Test() -> Unit:{newline}  {newline}  sleep(){newline}  {newline}  {newline}"));
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
