using System;

namespace ARESService.Services.Authentication;

public record InternalToken(string Token, DateTime Expiration);
