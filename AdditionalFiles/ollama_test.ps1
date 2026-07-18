$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$imagePath = "D:\test.png"
if (-not (Test-Path $imagePath)) {
    Write-Host "Error: File not found at $imagePath" -ForegroundColor Red
    Exit
}
$imageBytes = [System.IO.File]::ReadAllBytes($imagePath)
$imageBase64 = [Convert]::ToBase64String($imageBytes).Replace("`r", "").Replace("`n", "")

# Ультра-простой официальный промпт для Moondream
$jsonString = '{"model":"moondream","stream":false,"prompt":"What is in this image?","images":["' + $imageBase64 + '"]}'

try {
    $response = Invoke-RestMethod -Method Post -Uri "http://localhost:11434/api/generate" -Body $jsonString -ContentType "application/json; charset=utf-8" -TimeoutSec 60
    
    # Печатаем ВЕСЬ сырой ответ для детального анализа
    Write-Host "RAW RESPONSE FROM OLLAMA:" -ForegroundColor Cyan
    $response | ConvertTo-Json | Write-Host -ForegroundColor Gray

    if ($response.response) {
        Write-Host "VERDICT: " -NoNewline -ForegroundColor Green
        Write-Host $response.response.Trim() -ForegroundColor Yellow
    } else {
        Write-Host "Error: Response field is still empty." -ForegroundColor Red
    }
}
catch {
    Write-Host "Critical request error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor DarkRed
}
