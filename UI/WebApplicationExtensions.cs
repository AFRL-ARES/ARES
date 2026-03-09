using Ares.Core.Grpc;
using Ares.Services;
using Microsoft.Extensions.FileProviders;
using UI.Components.Formatting;
using UI.Application.Settings;
using UI.Infrastructure.Grpc;

namespace UI;

internal static class WebApplicationExtensions
{
  public static void UseAresUiPipeline(this WebApplication app)
  {
    var pluginPath = PluginPathResolver.Resolve(app.Configuration.Get<AppSettings>());
    if(Directory.Exists(pluginPath))
    {
      app.UseStaticFiles(new StaticFileOptions
      {
        FileProvider = new PhysicalFileProvider(pluginPath),
        RequestPath = "/plugins"
      });
    }

    app.UseStatusCodePagesWithReExecute("/404");
    app.UseHttpsRedirection();
    app.MapStaticAssets();
    app.UseRouting();
    app.UseAntiforgery();
  }

  public static void MapAresUiEndpoints(this WebApplication app)
  {
    app.MapCoreAresServices();
    app.MapAresServices();
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

    var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    appLifetime.ApplicationStopping.Register(OnStopping);
    appLifetime.ApplicationStopped.Register(OnStopped);

    app.Services.GetService<UnitCategoryHelper>();
  }

  private static void OnStopping()
  {
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse
    {
      ServerStatus = ServerStatus.Stopping,
      StatusMessage = "Server is stopping."
    });
  }

  private static void OnStopped()
  {
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse
    {
      ServerStatus = ServerStatus.Stopped,
      StatusMessage = "Server has been stopped."
    });
  }
}
