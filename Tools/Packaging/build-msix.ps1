param(
  [Parameter(Mandatory=$true)][string]$SourceDirectory,
  [Parameter(Mandatory=$true)][string]$OutputPath,
  [Parameter(Mandatory=$true)][string]$PackageName,
  [Parameter(Mandatory=$true)][string]$Publisher,
  [Parameter(Mandatory=$true)][string]$PublisherDisplayName,
  [Parameter(Mandatory=$true)][string]$Version,
  [Parameter(Mandatory=$true)][string]$RepositoryRoot
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch "^\d+\.\d+\.\d+\.\d+$") { throw "MSIX version must use four-part notation." }
$source = (Resolve-Path $SourceDirectory).Path
if (-not (Test-Path (Join-Path $source "Memory Trail.exe"))) { throw "Published Memory Trail executable not found." }
$staging = Join-Path (Split-Path $OutputPath -Parent) "msix-staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging | Out-Null
Copy-Item (Join-Path $source "*") $staging -Recurse -Force
$assetDir = Join-Path $staging "Assets"
python (Join-Path $RepositoryRoot "Tools\Packaging\prepare_msix_assets.py") $RepositoryRoot $assetDir
$template = Get-Content (Join-Path $RepositoryRoot "Packaging\MSIX\AppxManifest.template.xml") -Raw
function XmlEscape([string]$value) { return [System.Security.SecurityElement]::Escape($value) }
$manifest = $template.Replace("__PACKAGE_NAME__", (XmlEscape $PackageName)).Replace("__PUBLISHER__", (XmlEscape $Publisher)).Replace("__PUBLISHER_DISPLAY_NAME__", (XmlEscape $PublisherDisplayName)).Replace("__VERSION__", (XmlEscape $Version))
[System.IO.File]::WriteAllText((Join-Path $staging "AppxManifest.xml"), $manifest, [System.Text.UTF8Encoding]::new($false))
$makeAppx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter MakeAppx.exe | Where-Object { $_.FullName -match "\\x64\\MakeAppx\.exe$" } | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeAppx) { throw "MakeAppx.exe was not found." }
New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
if (Test-Path $OutputPath) { Remove-Item $OutputPath -Force }
& $makeAppx.FullName pack /d $staging /p $OutputPath /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE" }
Write-Host "Created MSIX: $OutputPath"
Get-FileHash $OutputPath -Algorithm SHA256
