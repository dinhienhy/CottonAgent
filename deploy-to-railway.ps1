# Script deploy CBAS lên Railway qua GitHub

Write-Host "=================================="
Write-Host "CBAS Railway Deployment via GitHub"
Write-Host "=================================="
Write-Host ""

# Step 1: Get GitHub username
Write-Host "Step 1: GitHub Configuration" -ForegroundColor Cyan
$githubUsername = Read-Host "Enter your GitHub username"
$repoName = "CottonAgent"

Write-Host ""
Write-Host "Repository will be: https://github.com/$githubUsername/$repoName" -ForegroundColor Yellow
$confirm = Read-Host "Is this correct? (y/n)"

if ($confirm -ne 'y') {
    Write-Host "Deployment cancelled." -ForegroundColor Red
    exit
}

# Step 2: Add GitHub remote
Write-Host ""
Write-Host "Step 2: Adding GitHub remote..." -ForegroundColor Cyan
git remote remove origin 2>$null
git remote add origin "https://github.com/$githubUsername/$repoName.git"

# Step 3: Rename branch to main
Write-Host ""
Write-Host "Step 3: Renaming branch to main..." -ForegroundColor Cyan
git branch -M main

# Step 4: Push to GitHub
Write-Host ""
Write-Host "Step 4: Pushing to GitHub..." -ForegroundColor Cyan
Write-Host "You may need to authenticate with GitHub" -ForegroundColor Yellow
Write-Host ""

git push -u origin main

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Failed to push to GitHub" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please make sure:" -ForegroundColor Yellow
    Write-Host "1. You have created the repository on GitHub: https://github.com/new" -ForegroundColor Yellow
    Write-Host "2. Repository name is: $repoName" -ForegroundColor Yellow
    Write-Host "3. You have GitHub authentication configured" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "After creating the repo, run this script again." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "SUCCESS: Code pushed to GitHub!" -ForegroundColor Green
Write-Host ""

# Step 5: Railway deployment instructions
Write-Host "=================================="
Write-Host "Next Steps: Deploy on Railway"
Write-Host "=================================="
Write-Host ""
Write-Host "1. Open Railway Dashboard:" -ForegroundColor Cyan
Write-Host "   https://railway.app/dashboard" -ForegroundColor White
Write-Host ""
Write-Host "2. Click 'New Project'" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Select 'Deploy from GitHub repo'" -ForegroundColor Cyan
Write-Host ""
Write-Host "4. Authorize Railway to access GitHub (if needed)" -ForegroundColor Cyan
Write-Host ""
Write-Host "5. Select repository: $githubUsername/$repoName" -ForegroundColor Cyan
Write-Host ""
Write-Host "6. Railway will auto-detect Dockerfile and deploy" -ForegroundColor Cyan
Write-Host ""
Write-Host "7. Add PostgreSQL:" -ForegroundColor Cyan
Write-Host "   - Click 'New' -> 'Database' -> 'Add PostgreSQL'" -ForegroundColor White
Write-Host ""
Write-Host "8. Link Database to App:" -ForegroundColor Cyan
Write-Host "   - Click service '$repoName'" -ForegroundColor White
Write-Host "   - Tab 'Variables'" -ForegroundColor White
Write-Host "   - Click '+ New Variable' -> 'Add Reference'" -ForegroundColor White
Write-Host "   - Select 'Postgres' -> 'DATABASE_URL'" -ForegroundColor White
Write-Host "   - Click 'Add'" -ForegroundColor White
Write-Host ""
Write-Host "9. Generate Domain:" -ForegroundColor Cyan
Write-Host "   - Click service '$repoName'" -ForegroundColor White
Write-Host "   - Tab 'Settings'" -ForegroundColor White
Write-Host "   - Section 'Networking' -> 'Generate Domain'" -ForegroundColor White
Write-Host ""
Write-Host "10. Wait for deployment (3-5 minutes)" -ForegroundColor Cyan
Write-Host ""
Write-Host "=================================="
Write-Host "Default Login Credentials"
Write-Host "=================================="
Write-Host "Username: admin" -ForegroundColor Yellow
Write-Host "Password: admin123" -ForegroundColor Yellow
Write-Host ""
Write-Host "IMPORTANT: Change password after first login!" -ForegroundColor Red
Write-Host ""

$openDashboard = Read-Host "Open Railway Dashboard now? (y/n)"
if ($openDashboard -eq 'y') {
    Start-Process "https://railway.app/dashboard"
}

Write-Host ""
Write-Host "Deployment preparation complete!" -ForegroundColor Green
