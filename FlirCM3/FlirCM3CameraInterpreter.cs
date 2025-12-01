using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using FlirCM3.Commands;
using FlirCM3.Enums;

namespace FlirCM3;

public class FlirCM3CameraInterpreter : DeviceCommandInterpreter<IFlirCM3Camera, FlirCM3CommandType>
{
  public FlirCM3CameraInterpreter(IFlirCM3Camera device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceId = Device.UniqueId,
        Name = FlirCM3CommandType.CaptureImage.ToString(),
        Description = "A command that tells the attached camera to capture a single image.",
        ParameterMetadatas = { new ParameterMetadata {Index = 0, Name = FlirCM3CommandParameter.SavePath.ToString(), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.String, true) } },
        OutputMetadata = new OutputMetadata()
        {
          Description = "Returns a byte array representing the image data.",
          DataSchema = AresSchemaHelper.CreateSchema("Image Data", AresDataType.ByteArray),
          Index = 0
        }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = FlirCM3CommandType.SetExposureTime.ToString(),
        Description = "A command that sets the exposure time of the attached camera.",
        ParameterMetadatas = { new ParameterMetadata {Index = 0, Name = FlirCM3CommandParameter.ExposureTime.ToString(), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) } }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = FlirCM3CommandType.GetLatestImagePath.ToString(),
        Description = "A command that gets the path to the latest captured image.",
        OutputMetadata = new OutputMetadata()
        {
          Description = "Returns a path to the latest image in the form of a string.",
          DataSchema = AresSchemaHelper.CreateSchema("Image Path", AresDataType.String)
        }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = FlirCM3CommandType.GetLatestImage.ToString(),
        Description = "A command that returns the latest captured image data in the form of a byte array.",
        OutputMetadata = new OutputMetadata()
        {
          Description = "A byte array that contains the latest image data.",
          DataSchema = AresSchemaHelper.CreateSchema("Image Data", AresDataType.ByteArray)
        }
      }
    };
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(FlirCM3CommandType deviceCommand, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = new CommandResult();

    switch(deviceCommand)
    {
      case FlirCM3CommandType.CaptureImage:
        var path = parameters.First(param => param.Metadata.Name.Equals($"{FlirCM3CommandParameter.SavePath}")).Value;

        var response = await Device.CaptureImage(path.StringValue);
        result.Success = true;
        result.Result = AresStructHelper.CreateBytesStruct("ImageData", response.ImageData.ToByteArray());
        break;

      case FlirCM3CommandType.SetExposureTime:
        var exposureTime = parameters.First(param => param.Metadata.Name.Equals($"{FlirCM3CommandParameter.ExposureTime}"));

        if(!exposureTime.Value.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The Flir CM3 Camera command SetExposureTime requires a number as a parameter, but none was provided!";
          break;
        }

        await Device.SetExposureTime(exposureTime.Value.NumberValue);
        result.Success = true;
        break;

      case FlirCM3CommandType.GetLatestImage:
        result.Success = true;
        result.Result = AresStructHelper.CreateBytesStruct("ImageData", Device.ImageData);
        break;

      case FlirCM3CommandType.GetDisplayImage:
        result.Success = true;
        result.Result = AresStructHelper.CreateBytesStruct("DisplayImage", Device.DisplayImageData);
        break;

      case FlirCM3CommandType.GetLatestImagePath:
        result.Success = true;
        result.Result = AresStructHelper.CreateStringStruct("ImagePath", Device.LatestImagePath);
        break;

      default:
        result.Success = false;
        result.Error = "Unknown command sent to CM3 Camera device. No action taken.";
        break;
    }

    return result;
  }
}
