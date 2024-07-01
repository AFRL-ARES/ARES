using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Messaging;

namespace Ares.Device;

public abstract class DeviceCommandInterpreter<TQualifiedDevice, TDeviceCommandEnum>
  : IDeviceCommandInterpreter<TQualifiedDevice>
  where TQualifiedDevice : IAresDevice
  where TDeviceCommandEnum : struct, Enum
{
  protected DeviceCommandInterpreter(TQualifiedDevice device)
  {
    Device = device;
  }

  public IEnumerable<CommandMetadata> CommandsToIndexedMetadatas()
  {
    var commandMetadatas = CommandsToMetadatas();
    foreach (var commandMetadata in commandMetadatas)
      OrderCommandMetadata(commandMetadata);

    return commandMetadatas;
  }

  public Func<CancellationToken, Task<DeviceCommandResult>> TemplateToDeviceCommand(CommandTemplate commandTemplate)
  {
    Task<DeviceCommandResult> QualifiedDeviceAction(CancellationToken token)
    {
      return RouteDeviceAction(commandTemplate, token);
    }

    return QualifiedDeviceAction;
  }

  public TQualifiedDevice Device { get; }

  // NOTE: The intent of this command is to prevent protobuf message exposure to extensions. 
  // We want this abstract class to handle as much conversion/routing of protobuf/db to
  // lib representations as possible, making it easier/obvious for extensions to "know what to do".
  // couldn't think of something better to say, but its a comment that will get deleted anyway.
  protected abstract Task<DeviceCommandResult> ParseAndPerformDeviceAction(TDeviceCommandEnum deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken);

  private Task<DeviceCommandResult> RouteDeviceAction(CommandTemplate commandTemplate, CancellationToken cancellationToken)
  {
    var deviceCommandEnum = Enum.Parse<TDeviceCommandEnum>(commandTemplate.Metadata.Name);
    var arguments = commandTemplate.Parameters.OrderBy(argument => argument.Index).ToArray();
    return ParseAndPerformDeviceAction(deviceCommandEnum, arguments, cancellationToken);
  }

  protected abstract CommandMetadata[] CommandsToMetadatas();

  private void OrderCommandMetadata(CommandMetadata commandMetadata)
  {
    var parameterMetadatasAscending = commandMetadata.ParameterMetadatas.ToArray();

    if (parameterMetadatasAscending.Length > 1)
    {
      if (parameterMetadatasAscending.All(parameterMetadata => parameterMetadata.Index == default))
      {
        parameterMetadatasAscending = parameterMetadatasAscending.Select
          (
            (parameter, index) => {
              parameter.Index = index;
              return parameter;
            }
          )
          .ToArray();
      }
      else
      {
        // Improperly ordered
        var distinctParameterIndexes =
          parameterMetadatasAscending
            .Select(parameter => parameter.Index)
            .Distinct()
            .ToArray();

        if (distinctParameterIndexes.Length != parameterMetadatasAscending.Length)
          throw new Exception($"{GetType().Name} error parsing {commandMetadata.Name} parameters, parameter Indexes are not distinct");
      }
    }

    parameterMetadatasAscending = parameterMetadatasAscending.OrderBy(parameterMetadata => parameterMetadata.Index)
      .ToArray();

    commandMetadata.ParameterMetadatas.Clear();
    commandMetadata.ParameterMetadatas.AddRange(parameterMetadatasAscending);
  }
}
