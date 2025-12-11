using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class SetUnitsCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public SetUnitsCommand(int pump, int units) : base($"{pump} set units {units}", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} set units {units}"))
  {
  }
}
