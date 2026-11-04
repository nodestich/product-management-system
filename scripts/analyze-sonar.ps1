<#
.SYNOPSIS
    Builds and tests the backend, then publishes the result to the local SonarQube server.

.DESCRIPTION
    Requires the analysis stack to be running:  docker compose --profile sonar up -d
    Requires a token in SONAR_TOKEN (environment variable or .env at the repository root).
#>
[CmdletBinding()]
param(
    [string]$SonarHostUrl = 'http://localhost:9000',
    [string]$ProjectKey = 'product-management-system'
)

$ErrorActionPreference = 'Stop'
$LASTEXITCODE = 0

function Invoke-Step {
    param(
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][scriptblock]$Command
    )

    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "'$Description' failed with exit code $LASTEXITCODE."
    }
}

function Get-SonarToken {
    param([string]$EnvFile)

    if ($env:SONAR_TOKEN) { return $env:SONAR_TOKEN }
    if (-not (Test-Path -LiteralPath $EnvFile)) { return $null }

    $line = Get-Content -LiteralPath $EnvFile |
        Where-Object { $_ -match '^\s*SONAR_TOKEN\s*=' } |
        Select-Object -First 1

    if (-not $line) { return $null }

    return ($line -replace '^\s*SONAR_TOKEN\s*=\s*', '').Trim().Trim('"').Trim("'")
}

function Wait-ForSonar {
    param(
        [Parameter(Mandatory)][string]$BaseUrl,
        [int]$Attempts = 60,
        [int]$DelaySeconds = 5
    )

    $statusUrl = "$BaseUrl/api/system/status"

    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            $status = (Invoke-RestMethod -Uri $statusUrl -TimeoutSec 5).status
            if ($status -eq 'UP') {
                Write-Host "    SonarQube is UP" -ForegroundColor Green
                return
            }
            Write-Host "    status is $status ($i/$Attempts)"
        } catch {
            Write-Host "    waiting for $statusUrl ($i/$Attempts)"
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    throw "SonarQube never reported UP at $statusUrl."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$backendRoot = Join-Path $repoRoot 'backend'
$solution = Join-Path $backendRoot 'ProductManagement.sln'
$resultsDir = (Join-Path $backendRoot 'TestResults') -replace '\\', '/'

# Generated code and the composition root carry no analysable logic.
$exclusions = '**/Migrations/**,**/*Designer.cs,**/AppDbContextModelSnapshot.cs,**/Program.cs'

$token = Get-SonarToken -EnvFile (Join-Path $repoRoot '.env')
if (-not $token) {
    throw 'SONAR_TOKEN is not set. Copy .env.example to .env and paste the token from My Account > Security.'
}

Wait-ForSonar -BaseUrl $SonarHostUrl

if (Test-Path -LiteralPath $resultsDir) {
    Remove-Item -LiteralPath $resultsDir -Recurse -Force
}

Push-Location $backendRoot
try {
    Invoke-Step 'Restoring dotnet tools' { dotnet tool restore }

    Invoke-Step 'Starting the SonarQube analysis' {
        dotnet sonarscanner begin `
            "/k:$ProjectKey" `
            '/n:Product Management System' `
            "/d:sonar.host.url=$SonarHostUrl" `
            "/d:sonar.token=$token" `
            "/d:sonar.cs.cobertura.reportsPaths=$resultsDir/**/coverage.cobertura.xml" `
            "/d:sonar.exclusions=$exclusions" `
            "/d:sonar.coverage.exclusions=$exclusions" `
            '/d:sonar.qualitygate.wait=true'
    }

    Invoke-Step 'Building the solution' {
        dotnet build $solution --configuration Release
    }

    Invoke-Step 'Running unit tests' {
        dotnet test 'tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj' `
            --configuration Release --no-build `
            --collect:'XPlat Code Coverage' `
            --results-directory "$resultsDir/unit"
    }

    Invoke-Step 'Running integration tests' {
        dotnet test 'tests/ProductManagement.IntegrationTests/ProductManagement.IntegrationTests.csproj' `
            --configuration Release --no-build `
            --collect:'XPlat Code Coverage' `
            --results-directory "$resultsDir/integration"
    }

    Invoke-Step 'Running specs' {
        dotnet test 'tests/ProductManagement.Specs/ProductManagement.Specs.csproj' `
            --configuration Release --no-build `
            --collect:'XPlat Code Coverage' `
            --results-directory "$resultsDir/specs"
    }

    Invoke-Step 'Completing the analysis and waiting for the quality gate' {
        dotnet sonarscanner end "/d:sonar.token=$token"
    }

    Write-Host ''
    Write-Host "Dashboard: $SonarHostUrl/dashboard?id=$ProjectKey" -ForegroundColor Green
} finally {
    Pop-Location
}
