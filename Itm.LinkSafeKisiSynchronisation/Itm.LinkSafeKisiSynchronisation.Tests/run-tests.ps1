# PowerShell script to run LinkSafe Kisi Synchronisation Unit Tests

Write-Host "Running LinkSafe Kisi Synchronisation Unit Tests..." -ForegroundColor Green
Write-Host ""

# Change to script directory
Set-Location $PSScriptRoot

Write-Host "Building the test project..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --configuration Release --verbosity normal --logger "console;verbosity=detailed"

Write-Host ""
Write-Host "Test run completed." -ForegroundColor Green
Read-Host "Press Enter to exit"
