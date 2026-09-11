# Rebuild PAKET PANIK APK lewat Unity CLI, lalu laporkan ukuran dan SHA256.
param([string]$Output = "Builds/Android/PaketPanik.apk")

$unity = "C:\Users\fathahnoor\AppData\Local\Unity\bin\unity.exe"
if (-not (Test-Path -LiteralPath $unity)) { Write-Error "Unity CLI tidak ditemukan di $unity"; exit 1 }
$env:NO_COLOR = "1"

$status = & $unity --no-banner command editor_status --json 2>$null | ConvertFrom-Json
if (-not $status.success) { Write-Error "Unity Editor tidak siap. Buka project dulu."; exit 1 }
Write-Host "Editor siap. Memulai build Android..."

$start = & $unity --no-banner command build --target Android --outputPath $Output --confirm true --json 2>$null | ConvertFrom-Json
if (-not $start.success) { Write-Error "Build gagal dimulai."; exit 1 }

while ($true) {
    Start-Sleep -Seconds 20
    $j = & $unity --no-banner command build_status --json 2>$null | ConvertFrom-Json
    $inner = $j.data.result | ConvertFrom-Json
    Write-Host "[$($inner.status)] $($inner.result)"
    if ($inner.status -eq "completed") { break }
}

if (Test-Path -LiteralPath $Output) {
    $f = Get-Item -LiteralPath $Output
    Write-Host "APK   : $($f.FullName)"
    Write-Host "Size  : $([math]::Round($f.Length / 1MB, 1)) MB"
    Write-Host "SHA256: $((Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash)"
} else {
    Write-Warning "Build selesai tetapi file APK tidak ditemukan di $Output"
}
