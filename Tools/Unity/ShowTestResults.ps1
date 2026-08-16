param(
  [string]$Path = "",
  [string]$ResultsDir = "",
  [switch]$FailedOnly,
  [int]$TopSlow = 0
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
  param([string]$StartPath)

  $p = (Resolve-Path -LiteralPath $StartPath).Path
  while ($true) {
    if (Test-Path -LiteralPath (Join-Path $p ".git")) { return $p }
    $parent = Split-Path -Parent $p
    if ($parent -eq $p -or [string]::IsNullOrWhiteSpace($parent)) { break }
    $p = $parent
  }
  return (Resolve-Path -LiteralPath $StartPath).Path
}

function Resolve-ResultsPath {
  param(
    [string]$ExplicitPath,
    [string]$ExplicitResultsDir
  )

  if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
    $p = (Resolve-Path -LiteralPath $ExplicitPath).Path
    if (-not (Test-Path -LiteralPath $p)) { throw "Results xml not found: $p" }
    return $p
  }

  $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
  $repoRoot = Resolve-RepoRoot -StartPath (Join-Path $scriptDir "..\..")

  $dir = $ExplicitResultsDir
  if ([string]::IsNullOrWhiteSpace($dir)) {
    $dir = Join-Path $repoRoot "TestResults"
  }

  if (-not (Test-Path -LiteralPath $dir)) {
    throw "Results directory not found: $dir (pass -Path or -ResultsDir)"
  }

  $latest = Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -ieq ".xml" } |
    Sort-Object -Property LastWriteTime -Descending |
    Select-Object -First 1

  if (-not $latest) { throw "No .xml results found in: $dir" }
  return $latest.FullName
}

function Get-TestCasesFromNUnitXml {
  param([xml]$Xml)

  $nodes = @()
  try {
    $nodes = $Xml.SelectNodes("//test-case")
  } catch {
    $nodes = @()
  }

  $cases = @()
  foreach ($n in $nodes) {
    $duration = 0.0
    $d = $n.GetAttribute("duration")
    if (-not [string]::IsNullOrWhiteSpace($d)) { [double]::TryParse($d, [ref]$duration) | Out-Null }

    $failureMsg = $null
    $stack = $null
    $failure = $n.SelectSingleNode("failure")
    if ($failure) {
      $m = $failure.SelectSingleNode("message")
      $s = $failure.SelectSingleNode("stack-trace")
      if ($m) { $failureMsg = ($m.InnerText -as [string]) }
      if ($s) { $stack = ($s.InnerText -as [string]) }
    }

    $cases += [pscustomobject]@{
      FullName  = $n.GetAttribute("fullname")
      Name      = $n.GetAttribute("name")
      ClassName = $n.GetAttribute("classname")
      Result    = $n.GetAttribute("result")
      DurationS = $duration
      Message   = $failureMsg
      Stack     = $stack
    }
  }
  return $cases
}

$resultsPath = Resolve-ResultsPath -ExplicitPath $Path -ExplicitResultsDir $ResultsDir
Write-Host ("[ShowTestResults] File: {0}" -f $resultsPath)

[xml]$xml = Get-Content -LiteralPath $resultsPath -ErrorAction Stop

$run = $xml.DocumentElement
if ($run -and $run.Name -eq "test-run") {
  $summary = [pscustomobject]@{
    Result  = $run.GetAttribute("result")
    Total   = $run.GetAttribute("total")
    Passed  = $run.GetAttribute("passed")
    Failed  = $run.GetAttribute("failed")
    Skipped = $run.GetAttribute("skipped")
    DurationS = $run.GetAttribute("duration")
  }
  Write-Host "[ShowTestResults] Summary:"
  $summary | Format-List | Out-String | ForEach-Object { $_.TrimEnd() } | ForEach-Object { if ($_ -ne "") { Write-Host $_ } }
}

$cases = Get-TestCasesFromNUnitXml -Xml $xml
if (-not $cases -or $cases.Count -eq 0) {
  throw "No <test-case> nodes found. Is this an NUnit XML result file?"
}

if ($FailedOnly) {
  $cases = $cases | Where-Object { $_.Result -match "Fail" }
}

Write-Host ""
Write-Host ("[ShowTestResults] Test cases: {0}" -f $cases.Count)

$table = $cases | Sort-Object -Property DurationS -Descending |
  Select-Object Result, DurationS, FullName

if ($TopSlow -gt 0) { $table = $table | Select-Object -First $TopSlow }

$table | Format-Table -AutoSize | Out-String | ForEach-Object { $_.TrimEnd() } | ForEach-Object { if ($_ -ne "") { Write-Host $_ } }

$failed = $cases | Where-Object { $_.Result -match "Fail" }
if ($failed -and $failed.Count -gt 0) {
  Write-Host ""
  Write-Host ("[ShowTestResults] Failures: {0}" -f $failed.Count)
  foreach ($f in $failed) {
    Write-Host ""
    Write-Host ("- {0}" -f $f.FullName)
    if (-not [string]::IsNullOrWhiteSpace($f.Message)) {
      Write-Host "  Message:"
      ($f.Message -split "`r?`n") | ForEach-Object { Write-Host ("    {0}" -f $_) }
    }
    if (-not [string]::IsNullOrWhiteSpace($f.Stack)) {
      Write-Host "  Stack:"
      ($f.Stack -split "`r?`n") | ForEach-Object { Write-Host ("    {0}" -f $_) }
    }
  }
}

