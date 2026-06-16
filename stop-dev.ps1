$procs = Get-Process -Name "AwardsFerm.Worker", "AwardsFerm.Api" -ErrorAction SilentlyContinue
if ($procs) {
    $procs | Stop-Process -Force
    Write-Host "Stopped: $($procs.ProcessName -join ', ')" -ForegroundColor Yellow
} else {
    Write-Host "No AwardsFerm API/Worker processes running." -ForegroundColor Gray
}

# dotnet run sometimes leaves a child Worker exe without the expected name
Get-CimInstance Win32_Process -Filter "Name = 'AwardsFerm.Worker.exe'" -ErrorAction SilentlyContinue |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Write-Host "Done. You can now rebuild and run Worker/API." -ForegroundColor Green
