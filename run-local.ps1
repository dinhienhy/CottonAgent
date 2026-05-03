# Script chạy CBAS local với Neon.tech database

Write-Host "=================================="
Write-Host "Starting CBAS Application"
Write-Host "=================================="
Write-Host ""

# Set DATABASE_URL
$env:DATABASE_URL="postgresql://neondb_owner:npg_T7vYb0gSexMA@ep-dark-tooth-ao4g7e60.c-2.ap-southeast-1.aws.neon.tech/neondb?sslmode=require"

Write-Host "Database: Neon.tech (Singapore)" -ForegroundColor Green
Write-Host "URL: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Login: admin / admin123" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Red
Write-Host ""

# Run app
dotnet run
