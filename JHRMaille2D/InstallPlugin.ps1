$ErrorActionPreference = 'Stop'
$taskSource = Join-Path $PSScriptRoot 'JHR.Panneaux.bundle'
if (-not (Test-Path -LiteralPath $taskSource)) { $taskSource = Join-Path (Split-Path -Parent $PSScriptRoot) 'JHR.Panneaux.bundle' }
$taskDestination = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\JHR.Panneaux.bundle'
if (-not (Test-Path -LiteralPath (Join-Path $taskSource 'PackageContents.xml'))) { throw 'Paquet incomplet' }
if (Test-Path -LiteralPath $taskDestination) {
  $taskBackup = Join-Path $PSScriptRoot ('Sauvegardes\bundle-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
  New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
  Copy-Item -LiteralPath $taskDestination -Destination $taskBackup -Recurse
}
New-Item -ItemType Directory -Path $taskDestination -Force | Out-Null
Get-ChildItem -LiteralPath $taskSource | Copy-Item -Destination $taskDestination -Recurse -Force
Write-Output ('Plugin installé : ' + $taskDestination)
Write-Output 'Redémarrer AutoCAD après sauvegarde des plans pour vérifier le chargement automatique. NETLOAD permet de tester la session courante.'
