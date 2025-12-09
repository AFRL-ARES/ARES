using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class ViewParameterCommand : ChemyxPumpCommandBase<ChemyxPumpResponse>
{
  public ViewParameterCommand() : base("view parameter", new ChemyxPumpResponseParser())
  {
  }
}
