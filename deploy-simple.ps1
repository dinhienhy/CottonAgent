Write-Host "=================================="
Write-Host "CBAS Heroku Deployment"
Write-Host "=================================="
Write-Host ""

Write-Host "Checking Heroku CLI..." -ForegroundColor Yellow
heroku --version

Write-Host ""
Write-Host "Step 1: Login to Heroku" -ForegroundColor Cyan
Write-Host "Browser will open for login..." -ForegroundColor Yellow
heroku login

Write-Host ""
Write-Host "Step 2: Create Heroku App" -ForegroundColor Cyan
$appName = Read-Host "Enter app name (or press Enter for auto-generated)"

if ([string]::IsNullOrWhiteSpace($appName)) {
    heroku create
} else {
    heroku create $appName
}

$herokuRemote = git remote get-url heroku 2>$null
if ($herokuRemote -match 'heroku\.com/(.+)\.git') {
    $appName = $matches[1]
    Write-Host "App created: $appName" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 3: Add PostgreSQL" -ForegroundColor Cyan
Write-Host "Adding mini plan..." -ForegroundColor Yellow
heroku addons:create heroku-postgresql:mini -a $appName

Write-Host ""
Write-Host "Step 4: Set Stack" -ForegroundColor Cyan
heroku stack:set container -a $appName

Write-Host ""
Write-Host "Step 5: Set Environment" -ForegroundColor Cyan
heroku config:set ASPNETCORE_ENVIRONMENT=Production -a $appName

Write-Host ""
Write-Host "Step 6: Deploy to Heroku" -ForegroundColor Cyan
Write-Host "This may take 3-5 minutes..." -ForegroundColor Yellow
git push heroku master

Write-Host ""
Write-Host "Step 7: Check Status" -ForegroundColor Cyan
heroku ps -a $appName
heroku pg:info -a $appName

Write-Host ""
Write-Host "=================================="  -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "App URL: https://$appName.herokuapp.com" -ForegroundColor Cyan
Write-Host "Login: admin / admin123" -ForegroundColor Cyan
Write-Host ""

$openApp = Read-Host "Open app in browser? (y/n)"
if ($openApp -eq 'y') {
    heroku open -a $appName
}

Write-Host ""
Write-Host "IMPORTANT: Change default password after first login!" -ForegroundColor Red
