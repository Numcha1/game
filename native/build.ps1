$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'download-tools.ps1')
Push-Location $PSScriptRoot
try {
    dotnet publish SuanPleng.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'คู่มือ.txt') -Destination release
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tools/CLOUDFlARED-LICENSE.txt') -Destination release
    Write-Host "Game ready: $PSScriptRoot\release\SuanPleng.exe"
} finally {
    Pop-Location
}
