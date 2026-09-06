param(
  [string]$CoiRoot = "F:\Steam\steamapps\common\Captain of Industry",
  [switch]$Deploy
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $CoiRoot)) {
  throw "Captain of Industry installation was not found: $CoiRoot"
}

$env:COI_ROOT = $CoiRoot
$project = Join-Path $PSScriptRoot '..\PriorityManager.csproj'
$arguments = @('build', $project, '-c', 'Release')
if ($Deploy) { $arguments += '-p:DeployToModsFolder=true' }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
