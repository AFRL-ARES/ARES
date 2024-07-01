using Ares.Messages;
using Grpc.Core;
using Radzen;

namespace UI.Authentication;

public class AresAuthenticationService
{
  private readonly AresAuthenticationState _aresAuthenticationState;
  private readonly Ares.Messages.Authentication.AuthenticationClient _authClient;
  private readonly NotificationService _notificationService;

  public AresAuthenticationService(NotificationService notificationService, Ares.Messages.Authentication.AuthenticationClient authClient, AresAuthenticationState aresAuthenticationState)
  {
    _notificationService = notificationService;
    _authClient = authClient;
    _aresAuthenticationState = aresAuthenticationState;
  }

  public async Task<AuthStatus> Authenticate(string user, string password)
  {
    // auth client does not get bound as it's really just used here and due to the nature of the remote service settings
    // which can change, we do not want to bind the client into the service collection. Also the auth service can be anonymous so
    // credentials are unnecessary
    try
    {
      var authenticationResponse = await _authClient.AuthenticateAsync(new AuthenticationRequest { UserName = user, Password = password });
      if (!authenticationResponse.Success)
      {
        _aresAuthenticationState.Authenticated = false;
        _aresAuthenticationState.UserName = string.Empty;
        return AuthStatus.Failed;
      }

      _aresAuthenticationState.Authenticated = true;
      _aresAuthenticationState.UserName = user;
      _aresAuthenticationState.Token = authenticationResponse.Token.TokenString;
      if (authenticationResponse.Token.Expiration is not null)
        _aresAuthenticationState.TokenExpiration = authenticationResponse.Token.Expiration.ToDateTime();

      return AuthStatus.Success;
    }
    catch (RpcException)
    {
      _notificationService.Messages.Add(new NotificationMessage { Severity = NotificationSeverity.Error, Detail = "The ARES service may not be running at the given address or there was trouble connecting.", Duration = 10000 });
    }

    return AuthStatus.ConnectionFailed;
  }
}
