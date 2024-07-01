using Ares.Fc2.Messages.DeviceStates;
using FC2Core;
using Microsoft.EntityFrameworkCore;

namespace FC2Tests;

public class DbIntegrationTests
{
  [OneTimeSetUp]
  public void OneTimeSetup()
  {
    var context = GetContext();
    context.Database.EnsureCreated();
  }

  [OneTimeTearDown]
  public void OneTimeTearDown()
  {
    var context = GetContext();
    context.Database.EnsureDeleted();
  }

  private FC2DbContext GetContext()
  {
    var options = new DbContextOptionsBuilder<FC2DbContext>()
      .UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=FC2Test;Integrated Security=True;Pooling=False;")
      .Options;
    return new FC2DbContext(options);
  }

  [Test]
  public async Task Test1()
  {
    var context = GetContext();
    var mfcState = new MfcState();
    mfcState.Id = "Test1";
    var temp = 68;
    var pressure = 419;
    var volumetricFlow = 61;
    //var setPoint = 62;

    mfcState.Temperature = temp;
    mfcState.AbsolutePressure = pressure;
    mfcState.VolumetricFlow = volumetricFlow;
    //mfcState.Setpoint = setPoint;
    context.MfcStates.Add(mfcState);
    await context.SaveChangesAsync();
    await context.DisposeAsync();
    context = GetContext();

    var state = await context.MfcStates.FirstAsync();
    Assert.That(state.AbsolutePressure.Value, Is.EqualTo(pressure));
    Assert.That(state.Temperature.Value, Is.EqualTo(temp));
    context.MfcStates.Remove(state);
    await context.SaveChangesAsync();
  }
}