#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$ImageTag,
    [string]$SemVerTag,
    [string]$Registry,
    [switch]$Push,
    [switch]$NoCache,
    [string[]]$Platforms = @('linux/amd64', 'linux/arm64'),
    [string]$VersionProjectPath = '.\src\StillHere.Web\StillHere.Web.csproj'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ImageName = 'still-here'
$DockerfilePath = '.\Dockerfile'

function Write-Header      { param([string]$Message) Write-Host "`n=== $Message ===" -ForegroundColor Cyan }
function Write-Success     { param([string]$Message) Write-Host $Message -ForegroundColor Green }
function Write-WarningLine { param([string]$Message) Write-Host $Message -ForegroundColor Yellow }
function Fail-Build        { param([string]$Message) Write-Host $Message -ForegroundColor Red; throw $Message }

function Get-GitVersionValues {
    param([string]$VersionProjectPath)

    try {
        $json = dotnet msbuild $VersionProjectPath -nologo -verbosity:quiet `
            -target:GetVersion `
            -getProperty:GitVersion_SemVer `
            -getProperty:GitVersion_InformationalVersion 2>$null

        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($json)) {
            $parsed = $json | ConvertFrom-Json
            $semVer = $parsed.Properties.GitVersion_SemVer
            $infoVer = $parsed.Properties.GitVersion_InformationalVersion
            if (-not [string]::IsNullOrWhiteSpace($semVer)) {
                return [pscustomobject]@{ SemVer = $semVer; InformationalVersion = $infoVer }
            }
        }
    }
    catch {
        Write-WarningLine "MSBuild GetVersion target failed: $($_.Exception.Message)"
    }

    Write-WarningLine 'Falling back to GitVersion CLI...'

    $cliCommand = Get-Command dotnet-gitversion -ErrorAction SilentlyContinue
    if (-not $cliCommand) { $cliCommand = Get-Command gitversion -ErrorAction SilentlyContinue }

    if ($cliCommand) {
        $cliJson = & $cliCommand.Source /output json /config .\GitVersion.yml 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($cliJson)) {
            $parsed = $cliJson | ConvertFrom-Json
            return [pscustomobject]@{ SemVer = $parsed.SemVer; InformationalVersion = $parsed.InformationalVersion }
        }
    }

    return $null
}

Push-Location $PSScriptRoot
try {
    Write-Header 'Checking prerequisites'

    docker info *> $null
    if ($LASTEXITCODE -ne 0) { Fail-Build 'Docker does not appear to be running.' }

    docker buildx inspect --bootstrap *> $null
    if ($LASTEXITCODE -ne 0) { Fail-Build 'docker buildx is not available, or the builder could not be bootstrapped.' }

    Write-Header 'Resolving version'
    $gitVersion = Get-GitVersionValues -VersionProjectPath $VersionProjectPath

    if (-not $gitVersion -and [string]::IsNullOrWhiteSpace($ImageTag)) {
        Fail-Build 'Could not resolve a version via GitVersion, and no -ImageTag was supplied.'
    }

    if ([string]::IsNullOrWhiteSpace($ImageTag))   { $ImageTag = $gitVersion.SemVer }
    if ([string]::IsNullOrWhiteSpace($SemVerTag))  { $SemVerTag = $gitVersion.SemVer }

    $buildVersion = if ($gitVersion) { $gitVersion.SemVer } else { $ImageTag }
    $buildInformationalVersion = if ($gitVersion) { $gitVersion.InformationalVersion } else { $ImageTag }

    Write-Success "Resolved version: $buildVersion ($buildInformationalVersion)"

    $tags = @($ImageTag, $SemVerTag, 'latest') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique

    $repository = if (-not [string]::IsNullOrWhiteSpace($Registry)) { "$Registry/$ImageName" } else { $ImageName }

    $shouldPush = $Push -and -not [string]::IsNullOrWhiteSpace($Registry)
    if ($Push -and [string]::IsNullOrWhiteSpace($Registry)) {
        Write-WarningLine '-Push was specified without -Registry; falling back to a local build.'
    }

    $revision = try { (git rev-parse HEAD) } catch { 'unknown' }

    $commonArgs = @(
        '--build-arg', "BUILD_VERSION=$buildVersion",
        '--build-arg', "BUILD_INFORMATIONAL_VERSION=$buildInformationalVersion",
        '--label', "org.opencontainers.image.version=$buildVersion",
        '--label', "org.opencontainers.image.revision=$revision",
        '--label', 'org.opencontainers.image.source=https://github.com/Syntax-Circus/still-here',
        '--file', $DockerfilePath
    )
    if ($NoCache) { $commonArgs += '--no-cache' }

    if ($shouldPush) {
        Write-Header "Building and pushing $repository ($($Platforms -join ', '))"

        $tagArgs = @()
        foreach ($tag in $tags) { $tagArgs += @('--tag', "${repository}:$tag") }

        $buildArgs = @('buildx', 'build', '--platform', ($Platforms -join ','), '--push') + $tagArgs + $commonArgs + @('.')

        & docker @buildArgs
        if ($LASTEXITCODE -ne 0) { Fail-Build 'docker buildx build (push) failed.' }

        Write-Success "Pushed: $(($tags | ForEach-Object { "${repository}:$_" }) -join ', ')"
    }
    else {
        Write-Header "Building locally $repository ($($Platforms -join ', '))"

        foreach ($platform in $Platforms) {
            $isPrimary = $platform -eq 'linux/amd64'
            $suffix = if ($isPrimary) { '' } else { '-' + ($platform -replace '^linux/', '') }

            $tagArgs = @()
            foreach ($tag in $tags) { $tagArgs += @('--tag', "${repository}:$tag$suffix") }

            $buildArgs = @('buildx', 'build', '--platform', $platform, '--load') + $tagArgs + $commonArgs + @('.')

            Write-Header "Platform: $platform"
            & docker @buildArgs
            if ($LASTEXITCODE -ne 0) { Fail-Build "docker buildx build (load) failed for platform $platform." }
        }

        Write-Success "Local images built: $($tags -join ', ') (non-amd64 platforms get a suffixed tag, e.g. -arm64)"
    }
}
finally {
    Pop-Location
}
