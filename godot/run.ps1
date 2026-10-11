param([string]$GodotPath = $env:GODOT_MONO)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -GodotPath $GodotPath
if (-not $GodotPath) {
    $command = Get-Command godot -ErrorAction SilentlyContinue
    if ($command) { $GodotPath = $command.Source }
    else { $GodotPath = (Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE 'Downloads') -Filter 'Godot_v4.7.2-stable_mono_win64_console.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName }
}
& $GodotPath --path $PSScriptRoot
