param(
    [string]$MigrationName,
    [string]$Configuration = "Debug",
    [string[]]$Providers = @("Sqlite", "Postgres", "SqlServer"),
    [string[]]$Contexts = @("AresDbContext", "AresIdentityContext"),
    [switch]$Force
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
    exit 1
}

$root = "../../../../"
$migrationProjectBase = "AresService.Migrations."
$startupProject = "../../../UI.csproj"
$solution = Join-Path $root "AresOS.slnx"

function Get-LatestMigrationName {
    param(
        [Parameter(Mandatory=$true)]
        [string]$MigrationsPath,
        [Parameter(Mandatory=$true)]
        [string]$Context
    )

    $latestMigration = Get-ChildItem -Path $MigrationsPath -Filter "*_$Context.cs" |
        Where-Object { $_.Name -notlike "*.Designer.cs" -and $_.Name -notlike "*ModelSnapshot.cs" } |
        Sort-Object Name -Descending |
        Select-Object -First 1

    if ($null -eq $latestMigration) {
        return $null
    }

    return $latestMigration.BaseName -replace '^\d+_', ''
}

# -----------------------------
# Build once before migrations
# -----------------------------
Write-Host "Building project in $Configuration mode..."
dotnet build $startupProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed - aborting migration removal."
    exit 1
}

# -----------------------------
# Loop through providers + contexts
# -----------------------------
foreach ($provider in $Providers) {
    foreach ($context in $Contexts) {

        $migrationDir = $migrationProjectBase + $provider

        # Construct migration project path
        $outputDir = Join-Path $root $migrationDir
        $outputDir = Resolve-Path $outputDir
        $outputProjFileName = $migrationDir + ".csproj"
        $outputProj = Join-Path $outputDir $outputProjFileName

        # Ensure directory exists
        if (-not (Test-Path $outputDir)) {
            $fullDestination = Resolve-Path $outputDir
            Write-Error "Migration destination not found $fullDestination"
            exit 1
        }

        if (-not [string]::IsNullOrWhiteSpace($MigrationName)) {
            $migrationsPath = Join-Path $outputDir "Migrations"
            $expectedMigrationName = $MigrationName + "_$context"
            $latestMigrationName = Get-LatestMigrationName -MigrationsPath $migrationsPath -Context $context

            if ($latestMigrationName -ne $expectedMigrationName) {
                Write-Host "Skipping $context ($provider): latest migration is '$latestMigrationName', expected '$expectedMigrationName'."
                continue
            }
        }

        Write-Host "=== Removing latest migration for $context ($provider) ==="

        $removeArgs = @(
            "ef",
            "migrations",
            "remove",
            "--no-build",
            "--project", $outputProj,
            "--startup-project", $startupProject,
            "--context", $context
        )

        if ($Force) {
            $removeArgs += "--force"
        }

        $removeArgs += @("--", "--provider", $provider)

        dotnet @removeArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Migration removal failed for $context ($provider)"
            exit 1
        }
    }
}

dotnet build $solution
