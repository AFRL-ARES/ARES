using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Exceptions;
using Ares.Core.Visualization;
using Ares.Core.Visualization.Managers;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Visualizing;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Grpc.Services;

public class VisualizerService : AresVisualizerManagementService.AresVisualizerManagementServiceBase
{
  private readonly IVisualizerRepo _visualizerRepo;
  private readonly IRemoteVisualizerManager _remoteVisualizerManager;
  private readonly ILogger<VisualizerService> _logger;

  public VisualizerService(IVisualizerRepo visualizerRepo, IRemoteVisualizerManager remoteVisualizerManager, ILogger<VisualizerService> logger)
  {
    _visualizerRepo = visualizerRepo;
    _remoteVisualizerManager = remoteVisualizerManager;
    _logger = logger;
  }

  public override async Task<GetAllVisualizersResponse> GetAllVisualizers(Empty request, ServerCallContext? context)
  {
    var response = new GetAllVisualizersResponse();
    var infos = await Task.WhenAll(_visualizerRepo.AvailableVisualizers.Select(GetInfo));
    response.Visualizers.AddRange(infos);
    return response;
  }

  public override async Task<AddRemoteVisualizerResponse> AddRemoteVisualizer(AddRemoteVisualizerRequest request, ServerCallContext? context)
  {
    try
    {
      await _remoteVisualizerManager.CreateVisualizer(request.Name, request.Url);
      return new AddRemoteVisualizerResponse { Success = true };
    }
    catch(Exception e)
    {
      _logger.LogError("Failed to add remote visualizer, error received in gRPC Visualizer Service implementation: {exception}", e.Message);
      return new AddRemoteVisualizerResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  public override async Task<UpdateRemoteVisualizerResponse> UpdateRemoteVisualizer(UpdateRemoteVisualizerRequest request, ServerCallContext? context)
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
      _logger.LogError("Exception trying to update remote visualizer. Could not find a config for a visualizer named {name}.", request.Name);
      return new UpdateRemoteVisualizerResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  public override async Task<Empty> RemoveRemoteVisualizer(RemoveRemoteVisualizerRequest request, ServerCallContext? context)
  {
    await _remoteVisualizerManager.RemoveVisualizer(request.VisualizerId);
    return new Empty();
  }

  public override Task<StateResponse> GetState(StateRequest request, ServerCallContext? context)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(request.Id);

    if(visualizer is null)
    {
      _logger.LogError("Failed to fetch a fresh state from visualizer service with the ID of {ID}.", request.Id);
      return Task.FromResult(new StateResponse
      {
        State = State.Error,
        StateMessage = "Unable to fetch visualizers state!"
      });
    }

    return Task.FromResult(new StateResponse
    {
      State = visualizer.VisualizerState,
      StateMessage = visualizer.StateMessage
    });
  }

  public override async Task<VisualizerInfoResponse> GetInfo(VisualizerInfoRequest request, ServerCallContext? context)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(request.VisualizerId);
    if(visualizer is null)
    {
      _logger.LogWarning("ARES was unable to fetch info about a visualizer service matching the ID of {ID}.", request.VisualizerId);
      return new VisualizerInfoResponse
      {
        Info = new VisualizerInfo { Name = "Unknown", Description = "Visualizer not found" }
      };
    }

    return new VisualizerInfoResponse { Info = await GetInfo(visualizer) };
  }

  public override Task<AresStruct> GetVisualizerSettings(VisualizerSettingsRequest request, ServerCallContext? context)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(request.VisualizerId);

    if(visualizer is null)
    {
      _logger.LogError("ARES failed to fetch the settings for a visualizer with the ID of {ID}, as one could not be found.", request.VisualizerId);
      return Task.FromResult(new AresStruct());
    }

    return Task.FromResult(visualizer.Settings);
  }

  public async Task<AresStructSchema> GetVisualizerSettingsSchema(string visualizerId)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerId);

    if(visualizer is null)
    {
      _logger.LogError("ARES failed to fetch the settings schema for a visualizer with the ID of {ID}, as one could not be found.", visualizerId);
      return new AresStructSchema();
    }

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

  public override async Task<Empty> SetVisualizerSettings(VisualizerSettings request, ServerCallContext? context)
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