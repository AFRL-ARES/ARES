param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName,
    [string]$Configuration = "Debug",
    [string[]]$Providers = @("Sqlite", "Postgres", "SqlServer")
)

try { dotnet | Out-Null }
catch {
    Write-Error "No dotnet installed."
    exit
}

dotnet ef *> $null

if (!$?)
{
    Write-Host "Dotnet tools may not have been installed. They are needed for this script."
    Write-Error $Error[0]
    pause
}

$project = "../AresService.csproj"
$contexts = @("AresDbContext", "AresIdentityContext")
$migrationsRoot = "Migrations"

# -----------------------------
# Build once before migrations
# -----------------------------
Write-Host "Building project in $Configuration mode..."
dotnet build $project --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed — aborting migrations."
    exit 1
}

# -----------------------------
# Loop through providers + contexts
# -----------------------------
foreach ($provider in $Providers) {
    foreach ($context in $contexts) {

        # Prefix migration name to isolate by provider
        $migrationFullName = "${provider}_${MigrationName}"

        # Construct migration output path (e.g., Migrations/Sqlite/AresDbContext)
        $outputDir = Join-Path $migrationsRoot "$provider/$context"

        # Ensure directory exists
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
            Write-Host "Created directory: $outputDir"
        }

        Write-Host "=== Adding migration '$migrationFullName' for $context ($provider) ==="

        dotnet ef migrations add $migrationFullName `
            --no-build `
            --project $project `
            --startup-project $project `
            --context $context `
            --output-dir $outputDir `
            -- --provider $provider

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Migration failed for $context ($provider)"
            exit 1
        }
    }
}

# -----------------------------
# Rebuild after migrations
# -----------------------------
Write-Host "`Rebuilding project after migrations..."
dotnet build $project --configuration $Configuration

Write-Host "`All migrations created successfully!"
