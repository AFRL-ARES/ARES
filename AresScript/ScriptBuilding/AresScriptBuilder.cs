namespace AresScript.ScriptBuilding;

public sealed class AresScriptBuilder : AresScriptBlockBuilder
{
  public AresScriptBuilder(int indentSize = 2)
    : base([], indentSize, ScriptBuilderCapabilities.Root)
  {
  }
}
