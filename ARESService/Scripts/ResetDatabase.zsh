#!/bin/zsh

dotnet ef database drop --context AresDbContext -f --no-build
dotnet ef migrations remove --context FC2IdentityContext --no-build --project ../FC2Core/FC2Core.csproj
dotnet ef migrations remove --context AresDbContext --no-build --project ../FC2Core/FC2Core.csproj
dotnet ef migrations add DatabaseInit --context AresDbContext --project ../FC2Core/FC2Core.csproj
dotnet ef migrations add DatabaseInit --context FC2IdentityContext --project ../FC2Core/FC2Core.csproj
dotnet ef database update --context AresDbContext
dotnet ef database update --context FC2IdentityContext