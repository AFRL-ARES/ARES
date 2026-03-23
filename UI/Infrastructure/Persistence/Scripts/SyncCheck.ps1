param(
    [string[]]$Providers = @("Sqlite", "Postgres", "SqlServer")
)

$contexts = @("AresDbContext", "AresIdentityContext")
# Resolve absolute path for the startup project (UI.csproj)
$startupProject = Resolve-Path "../../../UI.csproj"
$migrationProjectBase = "AresService.Migrations."

foreach ($provider in $Providers) {
    foreach ($context in $contexts) {
        Write-Host "--- Checking drift for $provider - $context ---"
        
        # Construct the path to the specific migration project
        $migrationProjPath = Resolve-Path "../../../../$migrationProjectBase$provider/$migrationProjectBase$provider.csproj"

        dotnet ef migrations has-pending-model-changes `
            --project "$migrationProjPath" `
            --startup-project "$startupProject" `
            --context $context `
            --no-build `
            -- --provider $provider

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Success: $provider is in sync." -ForegroundColor Green
        } else {
            Write-Warning "Drift detected: $provider has pending changes!"
        }
    }
}