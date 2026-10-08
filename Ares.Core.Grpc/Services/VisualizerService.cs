using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Exceptions;
using Ares.Core.Visualization;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Visualizing;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Ares.Core.Grpc.Services;

public class VisualizerService(IVisualizerRepo visualizerRepo, IRemoteVisualizerManager remoteVisualizerManager) : AresVisualizerManagementService.AresVisualizerManagementServiceBase
{
  private readonly IVisualizerRepo _visualizerRepo = visualizerRepo;
  private readonly IRemoteVisualizerManager _remoteVisualizerManager = remoteVisualizerManager;

  public override async Task<GetAllVisualizersResponse> GetAllVisualizers(Empty request, ServerCallContext context)
  {
    var response = new GetAllVisualizersResponse();
    var infos = await Task.WhenAll(_visualizerRepo.AvailableVisualizers.Select(GetInfo));
    response.Visualizers.AddRange(infos);
    return response;
  }

  public override async Task<AddRemoteVisualizerResponse> AddRemoteVisualizer(AddRemoteVisualizerRequest request, ServerCallContext context)
  {
    try
    {
      await _remoteVisualizerManager.CreateVisualizer(request.Name, request.Url);
      return new AddRemoteVisualizerResponse { Success = true };
    }
    catch(Exception e)
    {
      return new AddRemoteVisualizerResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  public override async Task<UpdateRemoteVisualizerResponse> UpdateRemoteVisualizer(UpdateRemoteVisualizerRequest request, ServerCallContext context)
  {
    try
    {
      var config = new VisualizerConfig
      {
        UniqueId = request.VisualizerId,
        Name = request.Name,
        Url = request.Url
      };
      await _remoteVisualizerManager.UpdateVisualizer(config);
      return new UpdateRemoteVisualizerResponse { Success = true };
    }
    catch(ItemNotFoundException e)
    {
      return new UpdateRemoteVisualizerResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  public override async Task<Empty> RemoveRemoteVisualizer(RemoveRemoteVisualizerRequest request, ServerCallContext context)
  {
    await _remoteVisualizerManager.RemoveVisualizer(request.VisualizerId);
    return new Empty();
  }

  public override Task<StateResponse> GetState(StateRequest request, ServerCallContext context)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(request.Id) 
      ?? throw new ItemNotFoundException(request.Id, typeof(IRemoteVisualizer), "Failed to get state as requested visualizer was not found");

    return Task.FromResult(new StateResponse
    {
      State = visualizer.VisualizerState,
      StateMessage = visualizer.StateMessage
    });
  }

  public override async Task<VisualizerInfoResponse> GetInfo(VisualizerInfoRequest request, ServerCallContext context)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(request.VisualizerId);
    if(visualizer is null)
    {
      return new VisualizerInfoResponse
      {
        Info = new VisualizerInfo { Name = "Unknown", Description = "Visualizer not found" }
      };
    }

    return new VisualizerInfoResponse { Info = await GetInfo(visualizer) };
  }

  public override Task<AresStruct> GetVisualizerSettings(VisualizerSettingsRequest request, ServerCallContext context)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(request.VisualizerId)
      ?? throw new ItemNotFoundException(request.VisualizerId, typeof(IRemoteVisualizer), "Failed to get settings as requested visualizer was not found");

    return Task.FromResult(visualizer.Settings);
  }

  public async Task<AresStructSchema> GetVisualizerSettingsSchema(string visualizerId)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerId)
      ?? throw new ItemNotFoundException(visualizerId, typeof(IRemoteVisualizer), "Failed to get settings schema as requested visualizer was not found");

    var options = await visualizer.GetVisualizationOptions();
    var schema = new AresStructSchema();

    foreach(var option in options.VisualizationOptions)
    {
      foreach(var field in option.VisualizationSettingsSchema.Fields)
      {
        if(!schema.Fields.ContainsKey(field.Key))
          schema.Fields.Add(field.Key, field.Value);
      }
    }

    return schema;
  }

  public override async Task<Empty> SetVisualizerSettings(VisualizerSettings request, ServerCallContext context)
  {
    if(_visualizerRepo.GetVisualizerById(request.VisualizerId) is not null)
      await _remoteVisualizerManager.UpdateVisualizerSettings(request);

    return new Empty();
  }

  private static Task<VisualizerInfo> GetInfo(IRemoteVisualizer visualizer)
  {
    return Task.FromResult(new VisualizerInfo
    {
      Name = visualizer.Name,
      Type = visualizer.Type,
      Version = visualizer.Version,
      Description = visualizer.Description,
      UniqueId = visualizer.UniqueId,
      Url = visualizer.Address.ToString()
    });
  }
}