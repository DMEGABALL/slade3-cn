# -----------------------------------------------------------------------------
# build-windows.ps1 - Build the localised SLADE on Windows and assemble a
# ready-to-use folder (exe + slade.pk3 + lang/).
#
# Prerequisites:
#   * Visual Studio 2022 (or newer) with the "Desktop development with C++"
#     workload (provides cl.exe, Ninja and CMake)
#   * vcpkg, with VCPKG_ROOT set, or checked out at one of the usual locations
#   * 7-Zip or zip in PATH (only needed to build slade.pk3)
#
# Usage (from a normal PowerShell window - the script sets up the VS
# developer environment itself):
#
#   powershell -ExecutionPolicy Bypass -File build-windows.ps1
#   powershell -ExecutionPolicy Bypass -File build-windows.ps1 -Preset win-x64-debug
#
# The first run will let vcpkg download and build all dependencies
# (wxWidgets, SFML, libarchive, ...), which can take a while.
# -----------------------------------------------------------------------------
param(
    [string]$Preset = "win-x64-release",
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"
$src = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path (Split-Path $src -Parent) "SLADE3-CN-release"
}

# --- Visual Studio ----------------------------------------------------------
function Get-VsInstallPath {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (!(Test-Path $vswhere)) { return $null }
    $path = & $vswhere -latest -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -property installationPath
    if ($LASTEXITCODE -ne 0) { return $null }
    return $path
}

$vsPath = Get-VsInstallPath
if (!$vsPath) {
    Write-Error "Visual Studio with the C++ toolset was not found. Install Visual Studio 2022 (Community is fine) with the 'Desktop development with C++' workload."
    exit 1
}

if (!(Get-Command cl.exe -ErrorAction SilentlyContinue)) {
    $devShell = Join-Path $vsPath "Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
    if (!(Test-Path $devShell)) {
        Write-Error "Could not find the Visual Studio developer shell module. Please run this script from a 'x64 Native Tools Command Prompt for VS 2022' instead."
        exit 1
    }
    Import-Module $devShell
    Enter-VsDevShell -VsInstallPath $vsPath -SkipAutomaticLocation -DevCmdArguments "-arch=x64 -host_arch=x64" | Out-Null
}

# --- vcpkg ------------------------------------------------------------------
if (!$env:VCPKG_ROOT) {
    $candidates = @(
        "C:\vcpkg",
        "C:\src\vcpkg",
        "C:\dev\vcpkg",
        (Join-Path $env:USERPROFILE "vcpkg"),
        (Join-Path $env:USERPROFILE "source\repos\vcpkg")
    )
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "scripts\buildsystems\vcpkg.cmake")) {
            $env:VCPKG_ROOT = $c
            break
        }
    }
}
if (!$env:VCPKG_ROOT) {
    Write-Error "vcpkg was not found. Clone https://github.com/microsoft/vcpkg, run bootstrap-vcpkg.bat, and set the VCPKG_ROOT environment variable."
    exit 1
}

$toolchain = Join-Path $env:VCPKG_ROOT "scripts\buildsystems\vcpkg.cmake"
if (!(Test-Path $toolchain)) {
    Write-Error "VCPKG_ROOT is set to '$env:VCPKG_ROOT' but '$toolchain' does not exist."
    exit 1
}

# --- cmake ------------------------------------------------------------------
if (!(Get-Command cmake.exe -ErrorAction SilentlyContinue)) {
    $vsCmake = Join-Path $vsPath "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
    if (Test-Path $vsCmake) {
        $env:PATH = (Split-Path $vsCmake -Parent) + ";" + $env:PATH
    } else {
        Write-Error "cmake was not found. Install CMake or use the Visual Studio developer prompt."
        exit 1
    }
}

# --- Configure & build ------------------------------------------------------
Write-Host "==> Configuring ($Preset)"
& cmake --preset $Preset
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Building"
& cmake --build (Join-Path $src "out/build/$Preset")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# --- Assemble release folder ------------------------------------------------
$dist = Join-Path $src "dist"
$exe = Join-Path $dist "SLADE.exe"
if (!(Test-Path $exe)) {
    Write-Error "Build finished, but '$exe' was not found."
    exit 1
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Copy-Item $exe $OutDir -Force

$pk3 = Join-Path $dist "slade.pk3"
if (Test-Path $pk3) {
    Copy-Item $pk3 $OutDir -Force
} else {
    Write-Warning "slade.pk3 was not built (no 7-Zip/zip found). Install 7-Zip and rebuild, or copy dist\res manually."
}

$lang = Join-Path $src "lang"
if (Test-Path $lang) {
    Copy-Item $lang (Join-Path $OutDir "lang") -Recurse -Force
}

Write-Host ""
Write-Host "Done. Release folder: $OutDir"
Write-Host "Run SLADE.exe from there; the Chinese UI is loaded from lang\zh_CN.txt."
