$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Wait-ApiHealth {
    param([int]$TimeoutSeconds = 60)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $res = Invoke-WebRequest -Uri 'http://localhost:8080/api/health' -UseBasicParsing -TimeoutSec 2
            if ($res.StatusCode -eq 200) { return $true }
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    return $false
}

Write-Host "Starting AwardsFerm (API + Worker + Web)..." -ForegroundColor Cyan

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root'; dotnet run --project src\AwardsFerm.Api\AwardsFerm.Api.csproj"
Write-Host "Waiting for API on http://localhost:8080 ..." -ForegroundColor Yellow
if (-not (Wait-ApiHealth)) {
    Write-Host "API did not become ready in time. Web UI will retry automatically." -ForegroundColor Yellow
}
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root'; dotnet run --project src\AwardsFerm.Worker\AwardsFerm.Worker.csproj"
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\src\AwardsFerm.Web'; npm run dev"

Write-Host ""
Write-Host "API:    http://localhost:8080" -ForegroundColor Green
Write-Host "Worker: http://localhost:8081" -ForegroundColor Green
Write-Host "UI:     http://localhost:5173" -ForegroundColor Green
