$ErrorActionPreference = 'Stop'
$version = '2026.10.0'
$expectedHash = '86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c'
$toolsDirectory = Join-Path $PSScriptRoot 'tools'
$binaryPath = Join-Path $toolsDirectory 'cloudflared.exe'
New-Item -ItemType Directory -Path $toolsDirectory -Force | Out-Null
if ((Test-Path -LiteralPath $binaryPath) -and ((Get-FileHash -LiteralPath $binaryPath -Algorithm SHA256).Hash -ieq $expectedHash)) {
    Write-Host "cloudflared $version verified."
    return
}
$temporaryPath = Join-Path $toolsDirectory ('cloudflared-' + [guid]::NewGuid().ToString('N') + '.download')
try {
    Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/download/$version/cloudflared-windows-amd64.exe" -OutFile $temporaryPath -UseBasicParsing
    if ((Get-FileHash -LiteralPath $temporaryPath -Algorithm SHA256).Hash -ine $expectedHash) { throw 'cloudflared SHA-256 mismatch. Build stopped.' }
    Move-Item -LiteralPath $temporaryPath -Destination $binaryPath -Force
    Write-Host "cloudflared $version downloaded and verified."
} finally {
    if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath }
}
