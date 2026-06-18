using Ares.Core.Device.State.Logging;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Tests.Device.State.Logging;

internal class DeviceStateQueryBuilderTests
{
  private const string SummaryId = "11111111-1111-1111-1111-111111111111";
  private const string CampaignId = "22222222-2222-2222-2222-222222222222";

  [Test]
  public async Task BuildQuery_FiltersCompletedCampaignBySummaryUniqueId()
  {
    var options = CreateContextOptions();
    await using(var context = new CoreDatabaseContext(options))
    {
      context.CampaignExecutionSummaries.Add(CreateCampaignSummary(SummaryId, CampaignId));
      context.Set<DeviceState>().AddRange(
        CreateState(DateTime.UnixEpoch.AddSeconds(1)),
        CreateState(DateTime.UnixEpoch.AddSeconds(20)));
      await context.SaveChangesAsync();
    }

    await using(var context = new CoreDatabaseContext(options))
    {
      var filter = new DeviceStateRequestFilter { CompletedCampaignId = SummaryId };
      var query = await DeviceStateQueryBuilder.BuildQuery<DeviceState>(filter, context);
      var states = await query.ToArrayAsync();

      Assert.That(states.Select(state => state.Timestamp), Is.EqualTo([Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(1))]));
    }
  }

  [Test]
  public async Task BuildQuery_FiltersCompletedCampaignByCampaignIdFallback()
  {
    var options = CreateContextOptions();
    await using(var context = new CoreDatabaseContext(options))
    {
      context.CampaignExecutionSummaries.Add(CreateCampaignSummary(SummaryId, CampaignId));
      context.Set<DeviceState>().AddRange(
        CreateState(DateTime.UnixEpoch.AddSeconds(1)),
        CreateState(DateTime.UnixEpoch.AddSeconds(20)));
      await context.SaveChangesAsync();
    }

    await using(var context = new CoreDatabaseContext(options))
    {
      var filter = new DeviceStateRequestFilter { CompletedCampaignId = CampaignId };
      var query = await DeviceStateQueryBuilder.BuildQuery<DeviceState>(filter, context);
      var states = await query.ToArrayAsync();

      Assert.That(states.Select(state => state.Timestamp), Is.EqualTo([Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(1))]));
    }
  }

  private static DbContextOptions<CoreDatabaseContext> CreateContextOptions()
  {
    return new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
  }

  private static CampaignExecutionSummary CreateCampaignSummary(string uniqueId, string campaignId)
  {
    return new CampaignExecutionSummary
    {
      UniqueId = uniqueId,
      CampaignId = campaignId,
      CampaignName = "Campaign",
      ExecutionInfo = new ExecutionInfo
      {
        TimeStarted = Timestamp.FromDateTime(DateTime.UnixEpoch),
        TimeFinished = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(10))
      }
    };
  }

  private static DeviceState CreateState(DateTime timestamp)
  {
    return new DeviceState
    {
      DeviceId = "device-id",
      Timestamp = Timestamp.FromDateTime(timestamp),
      Data = new AresStruct()
    };
  }
}
