param(
  [string]$ProjectPath = "",
  [string]$UnityPath = "",
  [ValidateSet("EditMode","PlayMode")]
  [string[]]$Platforms = @("EditMode"),
  [string]$TestFilter = "",
  [string]$TestCategory = "",
  [string]$ResultsDir = "",
  [switch]$NoGraphics
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

function Find-UnityExe {
  param(
    [string]$ExplicitUnityPath,
    [string]$ProjectPath
  )

  if (-not [string]::IsNullOrWhiteSpace($ExplicitUnityPath)) {
    if (Test-Path -LiteralPath $ExplicitUnityPath) {
      return (Resolve-Path -LiteralPath $ExplicitUnityPath).Path
    }
    throw "Unity.exe not found: $ExplicitUnityPath"
  }

  $projectVersion = $null
  if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
    $pv = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"
    if (Test-Path -LiteralPath $pv) {
      foreach ($line in (Get-Content -LiteralPath $pv -ErrorAction SilentlyContinue)) {
        if ($line -match '^\s*m_EditorVersion:\s*(\S+)\s*$') {
          $projectVersion = $Matches[1]
          break
        }
      }
    }
  }

  $candidates = @(
    "C:\Program Files\Unity\Hub\Editor",
    "C:\Program Files\Unity Hub\Editor"
  ) | Where-Object { Test-Path -LiteralPath $_ }

  if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
    foreach ($base in $candidates) {
      $exe = Join-Path (Join-Path $base $projectVersion) "Editor\Unity.exe"
      if (Test-Path -LiteralPath $exe) { return $exe }
    }
  }

  foreach ($base in $candidates) {
    $dirs = Get-ChildItem -LiteralPath $base -Directory -ErrorAction SilentlyContinue |
      Sort-Object -Property Name -Descending

    foreach ($d in $dirs) {
      $exe = Join-Path $d.FullName "Editor\Unity.exe"
      if (Test-Path -LiteralPath $exe) { return $exe }
    }
  }

  $searchedRoots = if ($candidates.Count -gt 0) { ($candidates -join ", ") } else { "(no default roots found)" }
  if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
    throw "Unity.exe not found for project Unity version '$projectVersion'. Searched: $searchedRoots. Install via Unity Hub or pass -UnityPath."
  }
  throw "Unity.exe not found. Searched: $searchedRoots. Install via Unity Hub or pass -UnityPath."
}

function Ensure-Dir {
  param([string]$Path)
  if ([string]::IsNullOrWhiteSpace($Path)) { return }
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
  }
}

function Run-UnityTestsOnce {
  param(
    [string]$UnityExe,
    [string]$ProjectPath,
    [string]$Platform,
    [string]$ResultsPath,
    [string]$LogPath,
    [string]$ResultsDir,
    [string]$TestFilter,
    [string]$TestCategory,
    [bool]$NoGraphics
  )

  if (Test-Path -LiteralPath $LogPath) { Remove-Item -LiteralPath $LogPath -Force -ErrorAction SilentlyContinue }
  if (Test-Path -LiteralPath $ResultsPath) { Remove-Item -LiteralPath $ResultsPath -Force -ErrorAction SilentlyContinue }

  $args = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", $Platform,
    "-testResults", $ResultsPath,
    "-logFile", $LogPath
  )

  if ($NoGraphics) { $args += "-nographics" }
  if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { $args += @("-testFilter", $TestFilter) }
  if (-not [string]::IsNullOrWhiteSpace($TestCategory)) { $args += @("-testCategory", $TestCategory) }

  Write-Host ("[UnityTestRunner] Platform: {0}" -f $Platform)
  Write-Host ("[UnityTestRunner] Results:  {0}" -f $ResultsPath)
  Write-Host ("[UnityTestRunner] Log:      {0}" -f $LogPath)
  if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { Write-Host ("[UnityTestRunner] Filter:   {0}" -f $TestFilter) }
  if (-not [string]::IsNullOrWhiteSpace($TestCategory)) { Write-Host ("[UnityTestRunner] Category: {0}" -f $TestCategory) }

  $runStart = Get-Date
  $p = Start-Process -FilePath $UnityExe -ArgumentList $args -NoNewWindow -Wait -PassThru

  if ($p.ExitCode -ne 0) {
    Write-Host ("[UnityTestRunner] FAILED (Unity exit code={0}). Log tail:" -f $p.ExitCode)
    if (Test-Path -LiteralPath $LogPath) {
      Get-Content -LiteralPath $LogPath -Tail 200 | ForEach-Object { Write-Host $_ }
    } else {
      Write-Host "(log file missing)"
    }
    return $false
  }

  $actualResultsPath = $null
  if (Test-Path -LiteralPath $ResultsPath) {
    $actualResultsPath = $ResultsPath
  } else {
    $dir = if (-not [string]::IsNullOrWhiteSpace($ResultsDir)) { $ResultsDir } else { (Split-Path -Parent $ResultsPath) }
    if (-not [string]::IsNullOrWhiteSpace($dir) -and (Test-Path -LiteralPath $dir)) {
      $candidate = Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue |
        Where-Object {
          ($_.Extension -ieq ".xml") -and
          ($_.LastWriteTime -ge $runStart.AddSeconds(-2))
        } |
        Sort-Object -Property LastWriteTime -Descending |
        Select-Object -First 1
      if ($candidate) { $actualResultsPath = $candidate.FullName }
    }
  }

  if ([string]::IsNullOrWhiteSpace($actualResultsPath) -or -not (Test-Path -LiteralPath $actualResultsPath)) {
    Write-Host "[UnityTestRunner] FAILED (test results missing). Log tail:"
    if (Test-Path -LiteralPath $LogPath) {
      Get-Content -LiteralPath $LogPath -Tail 200 | ForEach-Object { Write-Host $_ }
    } else {
      Write-Host "(log file missing)"
    }
    return $false
  }

  if ($actualResultsPath -ne $ResultsPath) {
    Write-Host ("[UnityTestRunner] Note: Unity didn't write requested results path. Using detected results: {0}" -f $actualResultsPath)
  }

  try {
    [xml]$xml = Get-Content -LiteralPath $actualResultsPath -ErrorAction Stop
    $root = $xml.DocumentElement
    $failed = $null
    $result = $null

    if ($root -and $root.Name -eq "test-run") {
      $failed = $root.GetAttribute("failed")
      $result = $root.GetAttribute("result")
    } elseif ($root -and $root.Name -eq "testsuites") {
      $failed = $root.GetAttribute("failures")
      $result = $root.GetAttribute("result")
    }

    $failedInt = 0
    if (-not [string]::IsNullOrWhiteSpace($failed)) { [int]::TryParse($failed, [ref]$failedInt) | Out-Null }

    if ($failedInt -gt 0 -or ($result -and $result -match "Fail")) {
      Write-Host ("[UnityTestRunner] FAILED (results indicate failures). Failed={0} Result={1}" -f $failedInt, $result)
      return $false
    }
  } catch {
    Write-Host ("[UnityTestRunner] Warning: couldn't parse results xml ({0}). Treating as failure." -f $actualResultsPath)
    return $false
  }

  Write-Host ("[UnityTestRunner] OK (Unity exit code={0})" -f $p.ExitCode)
  return $true
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-RepoRoot -StartPath (Join-Path $scriptDir "..\..")

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
  $ProjectPath = $repoRoot
} else {
  $ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
}

if ([string]::IsNullOrWhiteSpace($ResultsDir)) {
  $ResultsDir = Join-Path $ProjectPath "TestResults"
}
Ensure-Dir -Path $ResultsDir

$unityExe = Find-UnityExe -ExplicitUnityPath $UnityPath -ProjectPath $ProjectPath
Write-Host ("[UnityTestRunner] Unity:   {0}" -f $unityExe)
Write-Host ("[UnityTestRunner] Project: {0}" -f $ProjectPath)

$allOk = $true
foreach ($plat in $Platforms) {
  $stamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
  $resultsPath = Join-Path $ResultsDir ("{0}-results-{1}.xml" -f $plat.ToLowerInvariant(), $stamp)
  $logPath = Join-Path $ResultsDir ("{0}-{1}.log" -f $plat.ToLowerInvariant(), $stamp)

  $ok = Run-UnityTestsOnce `
    -UnityExe $unityExe `
    -ProjectPath $ProjectPath `
    -Platform $plat `
    -ResultsPath $resultsPath `
    -LogPath $logPath `
    -ResultsDir $ResultsDir `
    -TestFilter $TestFilter `
    -TestCategory $TestCategory `
    -NoGraphics ([bool]$NoGraphics)

  if (-not $ok) { $allOk = $false }
}

if (-not $allOk) { exit 1 }
exit 0

