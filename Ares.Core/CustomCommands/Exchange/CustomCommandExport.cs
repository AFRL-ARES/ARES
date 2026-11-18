using Ares.Datamodel.Automation;

namespace Ares.Core.CustomCommands.Exchange;

public sealed record CustomCommandExport(CustomCommandVersion Definition, string Json, string SuggestedFileName);