param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$generator = Join-Path $RepositoryRoot "Scripts\generate_windows_icon.py"
$master = Join-Path $RepositoryRoot "Assets\MemoryTrailIconMaster.png"

if (-not (Test-Path $master)) {
    throw "Approved Memory Trail master icon is missing: $master"
}

if (-not (Test-Path $generator)) {
    throw "Memory Trail icon generator is missing: $generator"
}

python $generator $RepositoryRoot
if ($LASTEXITCODE -ne 0) {
    throw "Memory Trail icon generation failed with exit code $LASTEXITCODE."
}

Write-Host "Generated all Memory Trail Windows icon assets from Assets\MemoryTrailIconMaster.png"
