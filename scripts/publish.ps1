param(
    [switch]$SingleFile,
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RepoRoot "src\CleanPulse\CleanPulse.csproj"
$OutputDir = Join-Path $RepoRoot "dist"

Write-Host "Publishing CleanPulse to $OutputDir ..." -ForegroundColor Cyan

if ($SelfContained) {
    dotnet publish $Project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $OutputDir
}
elseif ($SingleFile) {
    dotnet publish $Project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $OutputDir
}
else {
    dotnet publish $Project -c Release -o $OutputDir
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Done! Executable ready at: $(Join-Path $OutputDir 'CleanPulse.exe')" -ForegroundColor Green
