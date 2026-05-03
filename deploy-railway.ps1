Write-Host "=================================="
Write-Host "CBAS Railway Deployment"
Write-Host "=================================="
Write-Host ""

Write-Host "Step 1: Login to Railway" -ForegroundColor Cyan
Write-Host "Browser will open for login..." -ForegroundColor Yellow
railway login

Write-Host ""
Write-Host "Step 2: Initialize Project" -ForegroundColor Cyan
railway init

Write-Host ""
Write-Host "Step 3: Add PostgreSQL Database" -ForegroundColor Cyan
Write-Host "Select PostgreSQL from the menu" -ForegroundColor Yellow
railway add

Write-Host ""
Write-Host "Step 4: Deploy Application" -ForegroundColor Cyan
Write-Host "This may take 3-5 minutes..." -ForegroundColor Yellow
railway up

Write-Host ""
Write-Host "Step 5: Generate Domain" -ForegroundColor Cyan
railway domain

Write-Host ""
Write-Host "=================================="  -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Opening app in browser..." -ForegroundColor Cyan
railway open

Write-Host ""
Write-Host "Login: admin / admin123" -ForegroundColor Cyan
Write-Host "IMPORTANT: Change password after first login!" -ForegroundColor Red
