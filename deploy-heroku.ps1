# Script tự động deploy CBAS lên Heroku
# Chạy: .\deploy-heroku.ps1

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "CBAS Heroku Deployment Script" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra Heroku CLI
Write-Host "Checking Heroku CLI..." -ForegroundColor Yellow
try {
    $herokuVersion = heroku --version
    Write-Host "✓ Heroku CLI found: $herokuVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Heroku CLI not found!" -ForegroundColor Red
    Write-Host "Please install from: https://devcenter.heroku.com/articles/heroku-cli" -ForegroundColor Red
    exit 1
}

# Kiểm tra Git
Write-Host "Checking Git..." -ForegroundColor Yellow
try {
    $gitVersion = git --version
    Write-Host "✓ Git found: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Git not found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 1: Login to Heroku" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Opening browser for Heroku login..." -ForegroundColor Yellow
heroku login

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 2: Create Heroku App" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
$appName = Read-Host "Enter app name (or press Enter for auto-generated name)"

if ([string]::IsNullOrWhiteSpace($appName)) {
    Write-Host "Creating app with auto-generated name..." -ForegroundColor Yellow
    heroku create
} else {
    Write-Host "Creating app: $appName..." -ForegroundColor Yellow
    heroku create $appName
}

# Lấy app name từ git remote
$herokuRemote = git remote get-url heroku 2>$null
if ($herokuRemote -match 'heroku\.com/(.+)\.git') {
    $appName = $matches[1]
    Write-Host "✓ App created: $appName" -ForegroundColor Green
    Write-Host "URL: https://$appName.herokuapp.com" -ForegroundColor Green
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 3: Add PostgreSQL Database" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Note: PostgreSQL requires payment - 5 USD per month minimum" -ForegroundColor Yellow
Write-Host "Plans: mini or essential-0 - both 5 USD per month" -ForegroundColor Yellow
$dbPlan = Read-Host "Enter plan (mini/essential-0) [default: mini]"

if ([string]::IsNullOrWhiteSpace($dbPlan)) {
    $dbPlan = "mini"
}

Write-Host "Adding PostgreSQL $dbPlan..." -ForegroundColor Yellow
heroku addons:create heroku-postgresql:$dbPlan -a $appName

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 4: Set Stack to Container" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Setting stack to container..." -ForegroundColor Yellow
heroku stack:set container -a $appName

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 5: Set Environment Variables" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Setting ASPNETCORE_ENVIRONMENT..." -ForegroundColor Yellow
heroku config:set ASPNETCORE_ENVIRONMENT=Production -a $appName

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 6: Deploy to Heroku" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Pushing code to Heroku..." -ForegroundColor Yellow
Write-Host "This may take 3-5 minutes..." -ForegroundColor Yellow

git push heroku master

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Step 7: Check Deployment Status" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Checking app status..." -ForegroundColor Yellow
heroku ps -a $appName

Write-Host ""
Write-Host "Checking database..." -ForegroundColor Yellow
heroku pg:info -a $appName

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "App URL: https://$appName.herokuapp.com" -ForegroundColor Cyan
Write-Host "Login: admin / admin123" -ForegroundColor Cyan
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Yellow
Write-Host "  heroku logs --tail -a $appName" -ForegroundColor White
Write-Host "  heroku open -a $appName" -ForegroundColor White
Write-Host "  heroku restart -a $appName" -ForegroundColor White
Write-Host ""

$openApp = Read-Host "Open app in browser? (y/n)"
if ($openApp -eq 'y') {
    heroku open -a $appName
}

Write-Host ""
Write-Host "⚠️  IMPORTANT: Change default password after first login!" -ForegroundColor Red
Write-Host ""
