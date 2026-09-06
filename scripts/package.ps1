param(
  [string]$CoiRoot = "F:\Steam\steamapps\common\Captain of Industry",
  [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -CoiRoot $CoiRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
  New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$stagingRoot = Join-Path $env:TEMP 'PriorityManager-package'
$modRoot = Join-Path $stagingRoot 'PriorityManager'
Remove-Item -LiteralPath $stagingRoot -Force -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $modRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'manifest.json') -Destination $modRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'config.json') -Destination $modRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $modRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'bin\Release\net48\PriorityManager.dll') -Destination $modRoot

$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'manifest.json') -Raw | ConvertFrom-Json
$archive = Join-Path $OutputDirectory ("PriorityManager_{0}.zip" -f $manifest.version)
Compress-Archive -LiteralPath $modRoot -DestinationPath $archive -Force
Remove-Item -LiteralPath $stagingRoot -Force -Recurse
"Created $archive"
