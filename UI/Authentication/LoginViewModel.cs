using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ares.Messaging;
using DynamicData.Binding;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using ReactiveUI;
using UI.Properties;
using UI.Services.ServerHealth;
using UI.Settings;

namespace UI.Authentication;

internal class LoginViewModel : INotifyPropertyChanged
{
  private readonly AresAuthenticationService _aresAuthenticationService;
  private readonly RemoteServiceSettings _remoteServiceSettings;

  public LoginViewModel(IOptions<RemoteServiceSettings> serviceSettings, AresAuthenticationService aresAuthenticationService)
  {
    _aresAuthenticationService = aresAuthenticationService;
    _remoteServiceSettings = serviceSettings.Value;
    if (_remoteServiceSettings.ServerPort != null)
      ServerPort = _remoteServiceSettings.ServerPort.Value;

    var canLogin =
      this.WhenAnyPropertyChanged(
          nameof(UserName),
          nameof(Password),
          nameof(ServerHost),
          nameof(ServerPort)
        )
        .Select(vm => {
          if (vm is null) return false;

          return vm.UserName.Length > 0 && vm.Password.Length > 0 && vm.ServerHost?.Length > 0 && vm.ServerPort != 0;
        });

    TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnection);
    LoginCommand = ReactiveCommand.CreateFromTask(DoLogin, canLogin);
    Password = "123456";
    // if (!string.IsNullOrEmpty(UserName))
    //   LoginCommand.Execute(null);
  }

  public string UserName { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public bool PersistUserName { get; set; }

  public AuthStatus AuthStatus { get; set; } = AuthStatus.Unattempted;

  public string? ServerHost
  {
    get => _remoteServiceSettings.ServerHost;

    set => _remoteServiceSettings.ServerHost = value;
  }

  public int? ServerPort
  {
    get => _remoteServiceSettings.ServerPort;

    set => _remoteServiceSettings.ServerPort = value;
  }

  public AresConnectionStatus AresConnectionStatus { get; set; }

  public string? ServiceName { get; set; }

  public Version? ServiceVersion { get; set; }

  public ICommand LoginCommand { get; set; }

  public ICommand TestConnectionCommand { get; set; }

  public event PropertyChangedEventHandler? PropertyChanged;

  private async Task TestConnection()
  {
    AresConnectionStatus = AresConnectionStatus.Connecting;
    AresConnectionStatus = await GetConnectionStatus();
  }

  private async Task<AresConnectionStatus> GetConnectionStatus()
  {
    AresConnectionStatus aresConnectionStatus;
    var builder = new UriBuilder("https", ServerHost, ServerPort ?? 443);
    var channel = GrpcChannel.ForAddress(builder.Uri);
    var client = new AresServerInfo.AresServerInfoClient(channel);
    try
    {
      var info = await client.GetServerInfoAsync(new Empty());
      ServiceName = info.ServerName;
      ServiceVersion = Version.Parse(info.Version);
      aresConnectionStatus = AresConnectionStatus.Connected;
    }
    catch (Exception)
    {
      aresConnectionStatus = AresConnectionStatus.Disconnected;
      ServiceName = string.Empty;
      ServiceVersion = new Version();
    }

    return aresConnectionStatus;
  }

  private async Task DoLogin()
  {
    AuthStatus = await _aresAuthenticationService.Authenticate(UserName, Password);
  }

  // private void LoadUserName()
  // {
  //   bool boolResult = default;
  //   bool persistUserName = default;
  //   try
  //   {
  //     boolResult = bool.TryParse(ApplicationStorageHelper.LoadSetting(nameof(PersistUserName)), out persistUserName);
  //   }
  //   catch (InvalidOperationException)
  //   {
  //     boolResult = default;
  //   }
  //
  //   if (!boolResult
  //       || !persistUserName)
  //     return;
  //
  //   PersistUserName = true;
  //   UserName = ApplicationStorageHelper.LoadSetting(nameof(UserName));
  // }

  // private void SaveUserName()
  // {
  //   try
  //   {
  //     if (PersistUserName)
  //       ApplicationStorageHelper.SaveSetting(nameof(UserName), UserName);
  //     else
  //       ApplicationStorageHelper.RemoveSetting(nameof(UserName));
  //
  //     ApplicationStorageHelper.SaveSetting(nameof(PersistUserName), PersistUserName.ToString());
  //   }
  //   catch (InvalidOperationException)
  //   {
  //   }
  // }

  [NotifyPropertyChangedInvocator] protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
