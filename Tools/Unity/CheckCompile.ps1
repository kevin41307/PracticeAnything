param(
  [string]$ProjectPath = "",
  [string]$UnityPath = "",
  [string]$LogFile = ""
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

function Test-CompileErrorsInLog {
  param([string]$Path)

  if (-not (Test-Path -LiteralPath $Path)) { return $true }

  $patterns = @(
    "error CS\d+",
    "Compilation failed",
    "Script compile errors"
  )

  $content = Get-Content -LiteralPath $Path -ErrorAction SilentlyContinue
  foreach ($line in $content) {
    foreach ($pat in $patterns) {
      if ($line -match $pat) { return $true }
    }
  }
  return $false
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-RepoRoot -StartPath (Join-Path $scriptDir "..\..")

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
  $ProjectPath = $repoRoot
} else {
  $ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
  $LogFile = Join-Path ([System.IO.Path]::GetTempPath()) ("unity-compile-{0}.log" -f ([System.Guid]::NewGuid().ToString("N")))
} else {
  $LogFile = (Resolve-Path -LiteralPath (Split-Path -Parent $LogFile)).Path + "\" + (Split-Path -Leaf $LogFile)
}

$unityExe = Find-UnityExe -ExplicitUnityPath $UnityPath -ProjectPath $ProjectPath

Write-Host ("[UnityCompileCheck] Unity: {0}" -f $unityExe)
Write-Host ("[UnityCompileCheck] Project: {0}" -f $ProjectPath)
Write-Host ("[UnityCompileCheck] Log: {0}" -f $LogFile)

if (Test-Path -LiteralPath $LogFile) { Remove-Item -LiteralPath $LogFile -Force -ErrorAction SilentlyContinue }

$args = @(
  "-batchmode",
  "-quit",
  "-nographics",
  "-projectPath", $ProjectPath,
  "-logFile", $LogFile
)

$p = Start-Process -FilePath $unityExe -ArgumentList $args -NoNewWindow -Wait -PassThru

$hasCompileErrors = Test-CompileErrorsInLog -Path $LogFile
if ($hasCompileErrors) {
  Write-Host "[UnityCompileCheck] Compile errors detected (or log unreadable). Log tail:"
  if (Test-Path -LiteralPath $LogFile) {
    Get-Content -LiteralPath $LogFile -Tail 120
  } else {
    Write-Host "(log file missing)"
  }
  exit 1
}

Write-Host ("[UnityCompileCheck] OK (Unity exit code={0})" -f $p.ExitCode)
exit 0
