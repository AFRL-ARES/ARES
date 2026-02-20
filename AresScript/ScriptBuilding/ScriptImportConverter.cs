using AresScript.Generated;

namespace AresScript.ScriptBuilding;

internal static class ScriptImportConverter
{
  public static List<ScriptNode> Convert(AresLangParser.ProgramContext program)
  {
    return ConvertStatements(program.statement(), ScriptBuilderCapabilities.Root);
  }

  private static List<ScriptNode> ConvertStatements(
    IEnumerable<AresLangParser.StatementContext> statements,
    ScriptBuilderCapabilities capabilities)
  {
    return statements.Select(statement => ConvertStatement(statement, capabilities)).ToList();
  }

  private static ScriptNode ConvertStatement(AresLangParser.StatementContext statement, ScriptBuilderCapabilities capabilities)
  {
    return statement switch
    {
      AresLangParser.SimpleStmtContext simple => ConvertSimpleStatement(simple.simpleStatement(), capabilities),
      AresLangParser.IfStmtContext ifStmt => ConvertIfStatement(ifStmt.ifStatement(), capabilities),
      AresLangParser.WhileStmtContext whileStmt => ConvertWhileStatement(whileStmt.whileStatement(), capabilities),
      AresLangParser.ForStmtContext forStmt => ConvertForStatement(forStmt.forStatement(), capabilities),
      AresLangParser.ParallelStmtContext parallel => ConvertParallelStatement(parallel.parallelStatement()),
      AresLangParser.LoopControlStmtContext loopControl => ConvertLoopControl(loopControl.loopControlStatement(), capabilities),
      AresLangParser.FuncControlStmtContext funcControl => ConvertFuncControl(funcControl.funcControlStatement(), capabilities),
      _ => new LineNode(statement.GetText())
    };
  }

  private static ScriptNode ConvertSimpleStatement(AresLangParser.SimpleStatementContext statement, ScriptBuilderCapabilities capabilities)
  {
    return statement switch
    {
      AresLangParser.AssignStmtContext assign =>
        new LineNode($"{assign.assignment().lvalue().GetText()} = {assign.assignment().expression().GetText()}"),
      AresLangParser.ExprStmtContext expr =>
        new LineNode(expr.expression().GetText()),
      AresLangParser.AssertStmtContext assert =>
        ConvertAssert(assert.assertStatement()),
      AresLangParser.FunctionDeclContext function =>
        ConvertFunctionDeclaration(function.functionDeclaration()),
      _ => new LineNode(statement.GetText())
    };
  }

  private static LineNode ConvertAssert(AresLangParser.AssertStatementContext assert)
  {
    var expressions = assert.expression();
    return expressions.Length == 1
      ? new LineNode($"assert {expressions[0].GetText()}")
      : new LineNode($"assert {expressions[0].GetText()}, {expressions[1].GetText()}");
  }

  private static BlockNode ConvertFunctionDeclaration(AresLangParser.FunctionDeclarationContext declaration)
  {
    var ids = declaration.ID();
    var functionName = ids[0].GetText();
    var parameters = ids.Skip(1).Select(id => id.GetText()).ToArray();
    var signature = parameters.Length == 0
      ? $"def {functionName}()"
      : $"def {functionName}({string.Join(", ", parameters)})";

    var bodyCapabilities = new ScriptBuilderCapabilities(AllowReturn: true, AllowLoopControl: false);
    var bodyNodes = ConvertStatements(declaration.funcBlock().statement(), bodyCapabilities);
    return new BlockNode(signature, bodyNodes);
  }

  private static IfNode ConvertIfStatement(AresLangParser.IfStatementContext ifStatement, ScriptBuilderCapabilities capabilities)
  {
    var conditions = ifStatement.expression();
    var blocks = ifStatement.block();
    var thenBranch = ConvertStatements(blocks[0].statement(), capabilities);

    var elifBranches = new List<ConditionalBranchNode>();
    for(var i = 1; i < conditions.Length; i++)
    {
      var elifStatements = ConvertStatements(blocks[i].statement(), capabilities);
      elifBranches.Add(new ConditionalBranchNode($"elif {conditions[i].GetText()}", elifStatements));
    }

    IReadOnlyList<ScriptNode>? elseBranch = null;
    if(blocks.Length > conditions.Length)
    {
      elseBranch = ConvertStatements(blocks[^1].statement(), capabilities);
    }

    return new IfNode(conditions[0].GetText(), thenBranch, elifBranches, elseBranch);
  }

  private static BlockNode ConvertWhileStatement(AresLangParser.WhileStatementContext whileStatement, ScriptBuilderCapabilities capabilities)
  {
    var bodyCapabilities = new ScriptBuilderCapabilities(capabilities.AllowReturn, AllowLoopControl: true);
    var bodyNodes = ConvertStatements(whileStatement.loopBlock().statement(), bodyCapabilities);
    return new BlockNode($"while {whileStatement.expression().GetText()}", bodyNodes);
  }

  private static BlockNode ConvertForStatement(AresLangParser.ForStatementContext forStatement, ScriptBuilderCapabilities capabilities)
  {
    var bodyCapabilities = new ScriptBuilderCapabilities(capabilities.AllowReturn, AllowLoopControl: true);
    var bodyNodes = ConvertStatements(forStatement.loopBlock().statement(), bodyCapabilities);
    return new BlockNode($"for {forStatement.ID().GetText()} in {forStatement.expression().GetText()}", bodyNodes);
  }

  private static ParallelNode ConvertParallelStatement(AresLangParser.ParallelStatementContext parallelStatement)
  {
    var expressions = parallelStatement.parallelBlock().expression().Select(expression => expression.GetText()).ToArray();
    return new ParallelNode(expressions);
  }

  private static LineNode ConvertLoopControl(AresLangParser.LoopControlStatementContext statement, ScriptBuilderCapabilities capabilities)
  {
    if(!capabilities.AllowLoopControl)
    {
      throw new InvalidOperationException("Cannot import loop control statement outside a loop block.");
    }

    return statement switch
    {
      AresLangParser.BreakStmtContext => new LineNode("break"),
      AresLangParser.ContinueStmtContext => new LineNode("continue"),
      _ => new LineNode(statement.GetText())
    };
  }

  private static LineNode ConvertFuncControl(AresLangParser.FuncControlStatementContext statement, ScriptBuilderCapabilities capabilities)
  {
    if(!capabilities.AllowReturn)
    {
      throw new InvalidOperationException("Cannot import return statement outside a function block.");
    }

    return statement is AresLangParser.ReturnStmtContext returnStmt
      ? new LineNode(returnStmt.expression() is null ? "return" : $"return {returnStmt.expression().GetText()}")
      : new LineNode(statement.GetText());
  }
}
