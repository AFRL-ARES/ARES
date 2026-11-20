using AresScript.Generated;

namespace Ares.Core.CustomCommands.Exchange;

public sealed class DeviceReferenceVisitor(List<CustomCommandDeviceReference> references) : AresLangBaseVisitor<object?>
{
  public override object? VisitMemberAccess(AresLangParser.MemberAccessContext context)
  {
    var path = GetPath(context);
    if(path is not null && path.Length >= 2 && string.Equals(path[0], "devices", StringComparison.Ordinal))
    {
      var reference = new CustomCommandDeviceReference
      {
        DeviceIdentifier = path[1],
        CommandIdentifier = path.Length >= 3 ? path[2] : null,
        Line = context.Start.Line,
        Column = context.Start.Column
      };
      if(!references.Any(existing => existing.Line == reference.Line && existing.Column == reference.Column))
        references.Add(reference);
    }

    return base.VisitMemberAccess(context);
  }

  private static string[]? GetPath(AresLangParser.ExpressionContext expression)
  {
    if(expression is AresLangParser.AtomExprContext atom && atom.atom() is AresLangParser.IdContext id)
      return [id.ID().GetText()];

    if(expression is not AresLangParser.MemberAccessContext member)
      return null;

    var parent = GetPath(member.expression());
    return parent is null ? null : [.. parent, member.ID().GetText()];
  }
}