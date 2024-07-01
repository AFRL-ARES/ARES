using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device;
using Ares.Messaging;

namespace SyringePumpNE1000
{
  public class SyringePumpInterpreter : DeviceCommandInterpreter<ISyringePump, Ares.SyringePump.Ne1000.Messaging.Commands>
  {
    public SyringePumpInterpreter(ISyringePump device) : base(device)
    {
    }

    protected override Task<DeviceCommandResult> ParseAndPerformDeviceAction(Ares.SyringePump.Ne1000.Messaging.Commands deviceCommandEnum, Parameter[] parameters,
      CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override CommandMetadata[] CommandsToMetadatas()
    {
      throw new NotImplementedException();
    }
  }
}
