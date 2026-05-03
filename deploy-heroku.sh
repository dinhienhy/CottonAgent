#!/bin/bash

# Script tự động deploy CBAS lên Heroku
# Chạy: chmod +x deploy-heroku.sh && ./deploy-heroku.sh

echo "=================================="
echo "CBAS Heroku Deployment Script"
echo "=================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Kiểm tra Heroku CLI
echo -e "${YELLOW}Checking Heroku CLI...${NC}"
if command -v heroku &> /dev/null; then
    echo -e "${GREEN}✓ Heroku CLI found: $(heroku --version)${NC}"
else
    echo -e "${RED}✗ Heroku CLI not found!${NC}"
    echo -e "${RED}Please install from: https://devcenter.heroku.com/articles/heroku-cli${NC}"
    exit 1
fi

# Kiểm tra Git
echo -e "${YELLOW}Checking Git...${NC}"
if command -v git &> /dev/null; then
    echo -e "${GREEN}✓ Git found: $(git --version)${NC}"
else
    echo -e "${RED}✗ Git not found!${NC}"
    exit 1
fi

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 1: Login to Heroku${NC}"
echo -e "${CYAN}==================================${NC}"
echo -e "${YELLOW}Opening browser for Heroku login...${NC}"
heroku login

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 2: Create Heroku App${NC}"
echo -e "${CYAN}==================================${NC}"
read -p "Enter app name (or press Enter for auto-generated): " APP_NAME

if [ -z "$APP_NAME" ]; then
    echo -e "${YELLOW}Creating app with auto-generated name...${NC}"
    heroku create
else
    echo -e "${YELLOW}Creating app: $APP_NAME...${NC}"
    heroku create $APP_NAME
fi

# Lấy app name từ git remote
HEROKU_REMOTE=$(git remote get-url heroku 2>/dev/null)
if [[ $HEROKU_REMOTE =~ heroku\.com/(.+)\.git ]]; then
    APP_NAME="${BASH_REMATCH[1]}"
    echo -e "${GREEN}✓ App created: $APP_NAME${NC}"
    echo -e "${GREEN}URL: https://$APP_NAME.herokuapp.com${NC}"
fi

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 3: Add PostgreSQL Database${NC}"
echo -e "${CYAN}==================================${NC}"
echo -e "${YELLOW}Note: PostgreSQL requires payment (\$5/month minimum)${NC}"
echo -e "${YELLOW}Plans: mini (\$5/month) or essential-0 (\$5/month)${NC}"
read -p "Enter plan (mini/essential-0) [default: mini]: " DB_PLAN

if [ -z "$DB_PLAN" ]; then
    DB_PLAN="mini"
fi

echo -e "${YELLOW}Adding PostgreSQL $DB_PLAN...${NC}"
heroku addons:create heroku-postgresql:$DB_PLAN -a $APP_NAME

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 4: Set Stack to Container${NC}"
echo -e "${CYAN}==================================${NC}"
echo -e "${YELLOW}Setting stack to container...${NC}"
heroku stack:set container -a $APP_NAME

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 5: Set Environment Variables${NC}"
echo -e "${CYAN}==================================${NC}"
echo -e "${YELLOW}Setting ASPNETCORE_ENVIRONMENT...${NC}"
heroku config:set ASPNETCORE_ENVIRONMENT=Production -a $APP_NAME

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 6: Deploy to Heroku${NC}"
echo -e "${CYAN}==================================${NC}"
echo -e "${YELLOW}Pushing code to Heroku...${NC}"
echo -e "${YELLOW}This may take 3-5 minutes...${NC}"

git push heroku master

echo ""
echo -e "${CYAN}==================================${NC}"
echo -e "${CYAN}Step 7: Check Deployment Status${NC}"
echo -e "${CYAN}==================================${NC}"
echo -e "${YELLOW}Checking app status...${NC}"
heroku ps -a $APP_NAME

echo ""
echo -e "${YELLOW}Checking database...${NC}"
heroku pg:info -a $APP_NAME

echo ""
echo -e "${GREEN}==================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}==================================${NC}"
echo ""
echo -e "${CYAN}App URL: https://$APP_NAME.herokuapp.com${NC}"
echo -e "${CYAN}Login: admin / admin123${NC}"
echo ""
echo -e "${YELLOW}Useful commands:${NC}"
echo -e "  heroku logs --tail -a $APP_NAME"
echo -e "  heroku open -a $APP_NAME"
echo -e "  heroku restart -a $APP_NAME"
echo ""

read -p "Open app in browser? (y/n): " OPEN_APP
if [ "$OPEN_APP" = "y" ]; then
    heroku open -a $APP_NAME
fi

echo ""
echo -e "${RED}⚠️  IMPORTANT: Change default password after first login!${NC}"
echo ""
