using System.Linq;
using Ares.Messaging;
using Microsoft.AspNetCore.Authorization;

namespace Ares.Core.Grpc;

public class AuthorizeRoles : AuthorizeAttribute
{
  public AuthorizeRoles(params AresUserType[] roles)
  {
    Roles = roles.Select(type => type.ToString()).Aggregate((type1, type2) => $"{type1}, {type2}");
  }
}
