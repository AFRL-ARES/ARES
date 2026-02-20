namespace AresScript.ScriptBuilding;

internal readonly record struct ScriptBuilderCapabilities(bool AllowReturn, bool AllowLoopControl)
{
  public static ScriptBuilderCapabilities Root { get; } = new(AllowReturn: false, AllowLoopControl: false);
}