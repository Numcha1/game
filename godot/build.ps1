param([string]$GodotPath = $env:GODOT_MONO)
$ErrorActionPreference = 'Stop'
if (-not $GodotPath) {
    $command = Get-Command godot -ErrorAction SilentlyContinue
    if ($command) { $GodotPath = $command.Source }
    else {
        $candidate = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE 'Downloads') -Filter 'Godot_v4.7.2-stable_mono_win64_console.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($candidate) { $GodotPath = $candidate.FullName }
    }
}
if (-not $GodotPath -or -not (Test-Path -LiteralPath $GodotPath)) { throw 'Set GODOT_MONO or pass -GodotPath to the Godot 4.7.2 Mono executable.' }
$GodotPath = (Resolve-Path -LiteralPath $GodotPath).Path
$packages = Join-Path (Split-Path $GodotPath) 'GodotSharp\Tools\nupkgs'
if (Test-Path -LiteralPath $packages) {
    $escaped = [System.Security.SecurityElement]::Escape($packages)
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Encoding UTF8 -Value ('<configuration><packageSources><clear/><add key="GodotLocal" value="' + $escaped + '"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>')
}
Push-Location $PSScriptRoot
try {
    & dotnet build SeasonsTogether.csproj
    if ($LASTEXITCODE -ne 0) { throw 'C# build failed.' }
    & $GodotPath --headless --path $PSScriptRoot --editor --import --quit
    if ($LASTEXITCODE -ne 0) { throw 'Godot asset import failed.' }
} finally { Pop-Location }
