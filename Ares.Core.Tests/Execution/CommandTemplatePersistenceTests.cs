using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Tests.Execution;

internal class CommandTemplatePersistenceTests
{
  [Test]
  public async Task CommandTemplate_LoadsDeviceCommandAndMetadata()
  {
    var options = new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    await using var context = new CoreDatabaseContext(options);
    var commandTemplate = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata
        {
          UniqueId = Guid.NewGuid().ToString(),
          Name = "Legacy Command",
          DeviceId = "device-1",
          DeviceType = "Pump",
          OutputMetadata = new OutputMetadata { UniqueId = Guid.NewGuid().ToString() }
        }
      }
    };
    var stepTemplate = new StepTemplate { UniqueId = Guid.NewGuid().ToString() };
    stepTemplate.CommandTemplates.Add(commandTemplate);
    context.StepTemplates.Add(stepTemplate);
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();

    var loadedTemplate = await context.CommandTemplates.SingleAsync();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(loadedTemplate.CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.DeviceCommand));
      Assert.That(loadedTemplate.DeviceCommand, Is.Not.Null);
      Assert.That(loadedTemplate.DeviceCommand.Metadata.Name, Is.EqualTo("Legacy Command"));
      Assert.That(loadedTemplate.DeviceCommand.Metadata.DeviceId, Is.EqualTo("device-1"));
    }
  }
}
