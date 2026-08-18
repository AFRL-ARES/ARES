using Ares.Datamodel;
using Ares.Datamodel.Factories;

namespace AresScript.Symbols;

public record AresScriptParameter(string Name, AresValueSchema Schema)
{
  public AresScriptParameter(string name, AresDataType type = AresDataType.Any)
    : this(name, AresSchemaBuilder.Entry(type).Build())
  {
  }

  public AresDataType Type => Schema.Type;
}
