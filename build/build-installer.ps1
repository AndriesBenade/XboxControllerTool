<#
    Produces a distributable Windows installer for XboxControllerTool.

        pwsh build\build-installer.ps1

    Output: artifacts\XboxControllerTool-<version>.msi
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'XboxControllerTool\XboxControllerTool.csproj'
$wxsPath = Join-Path $repositoryRoot 'installer\XboxControllerTool.wxs'
$artifactsDirectory = Join-Path $repositoryRoot 'artifacts'
$publishDirectory = Join-Path $artifactsDirectory 'publish'

[xml]$project = Get-Content $projectPath
$version = ($project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)

if (-not $version) {
    throw "No <Version> found in $projectPath."
}

Write-Host "Building XboxControllerTool $version ($Configuration, $RuntimeIdentifier)" -ForegroundColor Cyan

if (Test-Path $publishDirectory) {
    Remove-Item $publishDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

# Self-contained so the target machine needs no .NET runtime installed.
dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host 'Installing the WiX build tool...' -ForegroundColor Cyan
    dotnet tool install --global wix --version 5.*

    if ($LASTEXITCODE -ne 0) {
        throw 'Could not install the WiX tool. Install it manually with: dotnet tool install --global wix --version 5.*'
    }
}

$installerPath = Join-Path $artifactsDirectory "XboxControllerTool-$version.msi"

wix build $wxsPath `
    -arch x64 `
    -define "Version=$version" `
    -define "PublishDir=$publishDirectory" `
    -out $installerPath

if ($LASTEXITCODE -ne 0) {
    throw 'wix build failed.'
}

# Publish the finished installer into the tracked release folder. Only this artifact is committed;
# artifacts\ (the publish tree and intermediates) stays ignored.
$releaseDirectory = Join-Path $repositoryRoot 'Releases'
New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null

Get-ChildItem $releaseDirectory -Filter 'XboxControllerTool-*.msi' |
    Where-Object { $_.Name -ne "XboxControllerTool-$version.msi" } |
    Remove-Item -Force

$releasePath = Join-Path $releaseDirectory "XboxControllerTool-$version.msi"
Copy-Item $installerPath $releasePath -Force

Write-Host "Installer ready: $releasePath" -ForegroundColor Green
Write-Host "Commit this file to publish the download." -ForegroundColor Green
