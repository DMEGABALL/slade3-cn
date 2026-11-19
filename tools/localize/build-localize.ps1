# Builds Localize.exe (the helper that wraps UI string literals in
# slade::i18n::tr(...) so they can be translated at runtime).
#
# Requires only the .NET Framework compiler that ships with Windows.
#
# Usage:  powershell -ExecutionPolicy Bypass -File build-localize.ps1

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (!(Test-Path $csc)) {
    Write-Error "Could not find csc.exe (.NET Framework 4.x compiler)."
    exit 1
}

& $csc /nologo /out:"$PSScriptRoot\Localize.exe" "$PSScriptRoot\Localize.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built $PSScriptRoot\Localize.exe"
