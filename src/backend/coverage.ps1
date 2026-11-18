<#
.SYNOPSIS
  Runs the backend tests with coverage, writes a text summary and gates on line coverage (ADR-005, TASK-003).
.DESCRIPTION
  Without -Cobertura: runs dotnet test with the coverlet collector, then finds the Cobertura files.
  With -Cobertura: skips the tests and only reports and gates on the given file(s).
  Scope counted: MiniWms.Domain, MiniWms.Application, MiniWms.Api (combined line coverage).
  Exit code 0 = gate passed, 1 = below threshold or a step failed.
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1 -Cobertura "src/backend/TestResults/*/coverage.cobertura.xml"
.NOTES
  Why -ExecutionPolicy Bypass: the Windows default policy blocks .ps1 files; the flag applies to that one run only.
  A relative -Cobertura or -ReportDir is resolved against the caller's folder; absolute paths are used as they are.
.PARAMETER Threshold
  Minimum combined line coverage in percent. Default 90.
.PARAMETER Cobertura
  Path or glob of Cobertura file(s). When given, the tests are not run.
.PARAMETER ReportDir
  Folder for the text summary. Default TestResults/coverage-report next to this script.
#>
[CmdletBinding()]
param(
    [double]$Threshold = 90,
    [string]$Cobertura,
    [string]$ReportDir
)

$ErrorActionPreference = 'Stop'

# Capture the caller's folder before any Set-Location; relative parameters resolve against it.
$callerDir = (Get-Location).ProviderPath
if ($Cobertura -and -not [System.IO.Path]::IsPathRooted($Cobertura)) { $Cobertura = Join-Path $callerDir $Cobertura }
if ($ReportDir -and -not [System.IO.Path]::IsPathRooted($ReportDir)) { $ReportDir = Join-Path $callerDir $ReportDir }

$scriptDir = $PSScriptRoot
if (-not $ReportDir) { $ReportDir = Join-Path $scriptDir 'TestResults\coverage-report' }
$resultsDir = Join-Path $scriptDir 'TestResults'
$assemblies = @('MiniWms.Domain', 'MiniWms.Application', 'MiniWms.Api')

# Tools may not be on PATH in agent shells.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet not found on PATH; prepending C:\Program Files\dotnet"
    $env:Path = 'C:\Program Files\dotnet;' + $env:Path
}

# The tool manifest (.config/dotnet-tools.json) is found by walking up from here.
Set-Location $scriptDir

if ($Cobertura) {
    Write-Host "Skipping tests; using Cobertura file(s): $Cobertura"
    $files = @(Get-ChildItem -Path $Cobertura -File -ErrorAction SilentlyContinue)
} else {
    if (Test-Path $resultsDir) {
        Write-Host "Removing old results in $resultsDir"
        Remove-Item -Recurse -Force $resultsDir
    }
    Write-Host "Running: dotnet test $scriptDir with coverage"
    & dotnet test $scriptDir --collect:"XPlat Code Coverage" --settings (Join-Path $scriptDir 'coverage.runsettings') --results-directory $resultsDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet test failed (exit code $LASTEXITCODE); no coverage gate was evaluated."
        exit 1
    }
    $files = @(Get-ChildItem -Path $resultsDir -Recurse -Filter 'coverage.cobertura.xml' -File)
}

if ($files.Count -eq 0) {
    Write-Host "No Cobertura files found. Cannot compute coverage."
    exit 1
}
Write-Host "Found $($files.Count) Cobertura file(s):"
$files | ForEach-Object { Write-Host "  $($_.FullName)" }

Write-Host "Restoring local tools"
& dotnet tool restore
if ($LASTEXITCODE -ne 0) { Write-Host "dotnet tool restore failed (exit code $LASTEXITCODE)."; exit 1 }

New-Item -ItemType Directory -Force $ReportDir | Out-Null
$reports = ($files | ForEach-Object { $_.FullName }) -join ';'
$filters = (($assemblies | ForEach-Object { "+$_" }) -join ';')
Write-Host "Building text summary in $ReportDir"
& dotnet tool run reportgenerator "-reports:$reports" "-targetdir:$ReportDir" '-reporttypes:TextSummary' "-assemblyfilters:$filters"
if ($LASTEXITCODE -ne 0) { Write-Host "reportgenerator failed (exit code $LASTEXITCODE)."; exit 1 }

$summaryFile = Join-Path $ReportDir 'Summary.txt'
if (-not (Test-Path $summaryFile)) { Write-Host "Summary.txt was not written to $ReportDir."; exit 1 }
$lines = @(Get-Content $summaryFile)
Write-Host ""
$lines | ForEach-Object { Write-Host $_ }
Write-Host ""

# Combined numbers come from the Summary section (covered / coverable lines).
$covered = $null
$coverable = $null
foreach ($l in $lines) {
    if ($l -match '^\s*Covered lines:\s*(\d+)') { $covered = [int]$Matches[1] }
    if ($l -match '^\s*Coverable lines:\s*(\d+)') { $coverable = [int]$Matches[1] }
}

# Per-assembly lines are the unindented "<name>  <percent>" rows.
Write-Host "Per-assembly line coverage:"
foreach ($a in $assemblies) {
    $row = $lines | Where-Object { $_ -match ('^' + [regex]::Escape($a) + '\s+(\S+)\s*$') } | Select-Object -First 1
    if ($row) {
        $null = $row -match ('^' + [regex]::Escape($a) + '\s+(\S+)\s*$')
        Write-Host ("  {0}: {1}" -f $a, $Matches[1])
    } else {
        Write-Host ("  {0}: n/a" -f $a)
    }
}

if ($null -eq $covered -or $null -eq $coverable) {
    Write-Host "Could not read covered/coverable lines from $summaryFile."
    exit 1
}
if ($coverable -eq 0) {
    Write-Host "No assembly has coverable lines yet; the gate passes (nothing to measure)."
    exit 0
}

$actual = [math]::Round(100.0 * $covered / $coverable, 2)
Write-Host ("Combined line coverage: {0}% ({1} of {2} lines); threshold {3}%" -f $actual, $covered, $coverable, $Threshold)
if (($covered * 100.0 / $coverable) -lt $Threshold) {
    $shortfall = [math]::Round($Threshold - (100.0 * $covered / $coverable), 2)
    Write-Host ("GATE FAILED: line coverage {0}% is below the {1}% threshold (shortfall {2} percentage points)." -f $actual, $Threshold, $shortfall)
    exit 1
}
Write-Host "GATE PASSED."
exit 0
