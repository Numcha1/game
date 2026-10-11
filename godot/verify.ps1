param([string]$GodotPath = $env:GODOT_MONO, [switch]$Rendered, [switch]$ExtendedLifecycle)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot
$reports = Join-Path $PSScriptRoot 'reports'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
if (-not $GodotPath) {
    $command = Get-Command godot -ErrorAction SilentlyContinue
    if ($command) { $GodotPath = $command.Source }
    else {
        $candidate = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE 'Downloads') -Filter 'Godot_v4.7.2-stable_mono_win64_console.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($candidate) { $GodotPath = $candidate.FullName }
    }
}
if (-not $GodotPath -or -not (Test-Path -LiteralPath $GodotPath)) { throw 'Set GODOT_MONO or pass -GodotPath.' }
$GodotPath = (Resolve-Path -LiteralPath $GodotPath).Path
function Invoke-Diagnostic([string[]]$Flags, [string]$Report, [int]$MinimumChecks) {
    $started = [DateTime]::UtcNow
    $arguments = @('--path', $PSScriptRoot)
    if (-not $Rendered) { $arguments += '--headless' }
    $arguments += '--'; $arguments += $Flags
    $previous = $ErrorActionPreference
    try {
        # Startup tests deliberately emit engine errors for malformed fixtures. The process code/report decide success.
        $ErrorActionPreference = 'Continue'
        & $GodotPath @arguments *> (Join-Path $reports ($Report + '.log'))
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    $path = Join-Path $reports $Report
    if ($code -ne 0 -or -not (Test-Path -LiteralPath $path)) { throw "Diagnostic failed: $Report" }
    if ((Get-Item -LiteralPath $path).LastWriteTimeUtc -lt $started) { throw "Stale diagnostic report: $Report" }
    $result = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($result.passed -lt $MinimumChecks) { throw "Incomplete diagnostic report: $Report" }
    Write-Host "$Report : $($result.passed) checks passed"
}
Push-Location $repository
try {
    & dotnet run --project godot/Tests/SeasonsTogether.Tests.csproj -- godot/reports/production-core-tests.json
    if ($LASTEXITCODE -ne 0) { throw 'Core regression failed.' }
    & (Join-Path $PSScriptRoot 'build.ps1') -GodotPath $GodotPath
    & dotnet build native/SuanPleng.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }
    $native = Join-Path $repository 'native/bin/Release/net8.0-windows/SuanPleng.exe'
    $process = Start-Process -FilePath $native -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw 'Native regression failed.' }
    Copy-Item -LiteralPath (Join-Path (Split-Path $native) 'self-test.json') -Destination (Join-Path $reports 'production-native-tests.json')
    $renderer = if ($Rendered) { 'Windows' } else { 'headless' }
    Invoke-Diagnostic @('--startup-checks') "startup-$renderer.json" 45
    Invoke-Diagnostic @('--recovery-check') "recovery-ui-$renderer.json" 10
    Invoke-Diagnostic @('--smoke') "godot-smoke-$renderer.json" $(if ($Rendered) { 101 } else { 26 })
    Invoke-Diagnostic @('--premium-checks') "premium-$renderer.json" $(if ($Rendered) { 175 } else { 47 })
    if ($ExtendedLifecycle) { Invoke-Diagnostic @('--lifecycle', '--lifecycle-extended') "lifecycle-extended-$renderer.json" 300 }
    else { Invoke-Diagnostic @('--lifecycle') "lifecycle-$renderer.json" 60 }
    Write-Host 'Verified local builds, isolated core/native regressions and Godot diagnostics. Internet, player playtest and export gates remain separate.'
} finally { Pop-Location }
