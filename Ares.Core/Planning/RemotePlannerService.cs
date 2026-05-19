using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Planning.Remote;
using Ares.Datamodel.Templates;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace Ares.Core.Planning;

public class RemotePlannerService : PlannerServiceBase
{
  private readonly GrpcChannel _channel;
  private PlannerServiceCapabilities _capabilities = new();

  public RemotePlannerService(string name, Uri address, string id) : base(name, "", "_._._", id)
  {
    _channel = GrpcChannel.ForAddress(address);
    Address = address;
  }

  public RemotePlannerService(string name, Uri address) : base(name, "", "_._._")
  {
    _channel = GrpcChannel.ForAddress(address);
    Address = address;
  }

  public override async Task Init()
  {
    await UpdateState();
    await UpdateInfo();
    await UpdateCapabilities();
  }

  internal async Task UpdateInfo()
  {
    var client = GetClient();
    try
    {
      var info = await client.GetInfoAsync(new Empty());
      Type = info.Name;
      Version = info.Version;
      Description = info.Description;
    }

    catch(RpcException)
    {
    }
  }

  internal Task SetOfflinePlannerStatus(string message)
  {
    PlannerServiceState = State.Inactive;
    StateMessage = message;
    return Task.CompletedTask;
  }

  internal async Task UpdateState()
  {
    var client = GetClient();
    try
    {
      var state = await client.GetStateAsync(new Empty());
      PlannerServiceState = state.State;
      StateMessage = state.StateMessage;
    }

    catch(RpcException e)
    {
      PlannerServiceState = State.Inactive;
      StateMessage = $"Failed to connect to planner: {e.Message}";
    }
  }

  internal async Task UpdateCapabilities()
  {
    if(PlannerServiceState != State.Active)
      return;

    var client = GetClient();
    try
    {
      _capabilities = await client.GetPlannerServiceCapabilitiesAsync(new Empty());
    }

    catch(RpcException)
    {
      return;
    }

    AvailablePlanners.Clear();
    AvailablePlanners.AddRange(_capabilities.AvailablePlanners);

    if(_capabilities.TimeoutSeconds > 0)
      PlanningTimeout = TimeSpan.FromSeconds(_capabilities.TimeoutSeconds);

    else
      PlanningTimeout = TimeSpan.MaxValue;

    var newSettings = _capabilities.SettingsSchema?.Fields.Where(entry => !Settings.Fields.ContainsKey(entry.Key)) ?? [];
    var removedSettings = Settings.Fields.Where(entry => !_capabilities.SettingsSchema?.Fields.ContainsKey(entry.Key) ?? false);

    foreach(var removedSetting in removedSettings)
    {
      Settings.Fields.Remove(removedSetting.Key);
    }

    foreach(var newSetting in newSettings)
    {
      if(newSetting.Value.Type == AresDataType.String)
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type, newSetting.Value.StringChoices?.Strings);
      }
      else if(newSetting.Value.Type == AresDataType.Number)
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type, newSetting.Value.NumberChoices?.Numbers);
      }
      else
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type);
      }
    }
  }

  public override async Task<PlannerServiceCapabilities> GetCapabilities(CancellationToken cancellationToken = default)
  {
    var client = GetClient();
    try
    {
      var capabilities = await client.GetPlannerServiceCapabilitiesAsync(new Empty(), cancellationToken: cancellationToken);
      _capabilities = capabilities;
      return capabilities;
    }

    catch(RpcException)
    {
      return _capabilities;
    }
  }

  public override async Task<PlanningResponse> Plan(PlanningRequest planRequest, CancellationToken cancellationToken = default)
  {
    var client = GetClient();
    planRequest.AdapterSettings = Settings;

    var result = await client.PlanAsync(planRequest, cancellationToken: cancellationToken);
    return result;
  }

  public override async Task<PlanningResponse> Plan(PlanningRequest planRequest, AresStruct settings, CancellationToken cancellationToken = default)
  {
    planRequest.AdapterSettings = settings;
    var client = GetClient();
    var result = await client.PlanAsync(planRequest, cancellationToken: cancellationToken);
    return result;
  }

  private AresRemotePlannerService.AresRemotePlannerServiceClient GetClient()
  {
    return new AresRemotePlannerService.AresRemotePlannerServiceClient(_channel);
  }

  internal async Task UpdateInfo(PlannerServiceInfo info)
  {
    Type = info.Type;
    Description = info.Description;
    Version = info.Version;
    _capabilities = info.Capabilities;
    await UpdateCapabilities();
  }

  public Uri Address { get; }
}
