using System.Threading;
using System;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ares.Datamodel;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Ares.Core.Grpc.Services;

public class AresServerInfoService : AresServerInfo.AresServerInfoBase
{
  [AllowAnonymous]
  public override Task<ServerInfoResponse> GetServerInfo(Empty request, ServerCallContext? context)
  {
    var serverInfo = new ServerInfoResponse
    {
      ServerName = Assembly.GetEntryAssembly()?.GetName().Name,
      Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
    };

    return Task.FromResult(serverInfo);
  }

  [AuthorizeRoles(AresUserType.AresUser)]
  public override async Task GetServerStatusStream(Empty request, IServerStreamWriter<ServerStatusResponse> responseStream, ServerCallContext? context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var observable = ServerStatusHelper.ServerStatusSubject.AsObservable();
    observable.Subscribe(response => responseStream.WriteAsync(response), () => {}, cancellationToken);
    await observable;
  }
}
