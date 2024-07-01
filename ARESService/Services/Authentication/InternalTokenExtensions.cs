using Ares.Messages;
using Google.Protobuf.WellKnownTypes;

namespace ARESService.Services.Authentication;

public static class InternalTokenExtensions
{
  public static Token Externalize(this InternalToken token)
  {
    var protoToken = new Token
    {
      TokenString = token.Token,
      Expiration = token.Expiration.ToTimestamp()
    };

    return protoToken;
  }
}
