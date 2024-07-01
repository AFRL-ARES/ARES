using Ares.Messages.DeviceStates;
using ARESCore;
using ARESMessaging.DeviceStateLogging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ARESCore.DeviceStateLoggers;
public static class DeviceStateFilterBuilder
{
  public static async Task<IQueryable<T>> BuildFilter<T>(StateRequest request, ARESDbContext dbContext) where T : class, IDeviceState
  {
    var states = dbContext.Set<T>().AsQueryable();
    if (request.Start is not null)
    {
      states = states.Where(state => state.Timestamp >= request.Start);
    }
    if (request.End is not null)
    {
      states = states.Where(state => state.Timestamp <= request.End);
    }
    if (request.DeviceIds.Any())
    {
      states = states.Where(state => request.DeviceIds.Contains(state.DeviceId));
    }
    if (!string.IsNullOrEmpty(request.CompletedCampaignId))
    {
      var completedCampaign = await dbContext.CampaignResults.FirstOrDefaultAsync(result => result.CampaignId == request.CompletedCampaignId);
      if (completedCampaign is not null)
        states = states.Where(state => state.Timestamp >= completedCampaign.ExecutionInfo.TimeStarted && state.Timestamp <= completedCampaign.ExecutionInfo.TimeFinished);
    }
    if (!string.IsNullOrEmpty(request.CompletedExperimentId))
    {
      var completedExperiment = await dbContext.CampaignResults
        .SelectMany(result => result.ExperimentResults)
        .FirstOrDefaultAsync(result => result.CompletedExperiment.UniqueId == request.CompletedExperimentId);

      if (completedExperiment is not null)
        states = states.Where(state => state.Timestamp >= completedExperiment.ExecutionInfo.TimeStarted && state.Timestamp <= completedExperiment.ExecutionInfo.TimeFinished);
    }

    return states;
  }
}
