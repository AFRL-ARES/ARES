using Ares.Device;
using Ares.Messaging;
using LindbergFurnace.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace
{
  public class TubeFurnaceInterpreter : DeviceCommandInterpreter<ITubeFurnace, TubeFurnaceCommand>
  {
    public TubeFurnaceInterpreter(ITubeFurnace device) : base(device)
    {
    }

    protected override Task<DeviceCommandResult> ParseAndPerformDeviceAction(TubeFurnaceCommand deviceCommandEnum, Parameter[] parameters,
      CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override CommandMetadata[] CommandsToMetadatas()
    {
      return Array.Empty<CommandMetadata>() ;
    }
  }
}
