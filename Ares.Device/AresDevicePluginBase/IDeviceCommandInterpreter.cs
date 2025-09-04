using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Device;

public interface IDeviceCommandInterpreter<out TQualifiedDevice>
  where TQualifiedDevice : IAresDevice
{
  TQualifiedDevice Device { get; }
  Func<CancellationToken, Task<CommandResult>> TemplateToDeviceCommand(CommandTemplate commandTemplate);
  IEnumerable<CommandMetadata> CommandsToIndexedMetadatas();
}
