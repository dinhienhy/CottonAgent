# 🚀 Hướng dẫn Deploy CBAS lên Heroku

## Yêu cầu

- ✅ Tài khoản Heroku (https://signup.heroku.com)
- ✅ Heroku CLI đã cài đặt
- ✅ Git đã cài đặt
- ✅ Credit card (Heroku yêu cầu để verify, dù dùng free tier)

## Bước 1: Cài đặt Heroku CLI

### Windows
```powershell
# Download và cài đặt từ:
# https://devcenter.heroku.com/articles/heroku-cli

# Hoặc dùng Chocolatey
choco install heroku-cli
```

### macOS
```bash
brew tap heroku/brew && brew install heroku
```

### Linux
```bash
curl https://cli-assets.heroku.com/install.sh | sh
```

Kiểm tra cài đặt:
```bash
heroku --version
```

## Bước 2: Login vào Heroku

```bash
heroku login
```

Trình duyệt sẽ mở, đăng nhập vào tài khoản Heroku của bạn.

## Bước 3: Tạo Heroku App

```bash
# Di chuyển vào thư mục project
cd c:/Users/dinhi/Dropbox/Workspace/CascadeProjects/CottonAgent

# Tạo app mới (tên app phải unique)
heroku create cotton-broker-system

# Hoặc để Heroku tự động tạo tên
heroku create
```

Lưu lại tên app, ví dụ: `cotton-broker-system`

## Bước 4: Thêm PostgreSQL Database

```bash
# Thêm PostgreSQL addon (Essential plan - $5/month)
heroku addons:create heroku-postgresql:essential-0 -a cotton-broker-system

# Hoặc dùng Mini plan ($5/month, ít storage hơn)
heroku addons:create heroku-postgresql:mini -a cotton-broker-system

# Kiểm tra database đã tạo
heroku addons -a cotton-broker-system
```

**Lưu ý**: Heroku đã ngừng free tier cho PostgreSQL. Plan rẻ nhất là Mini ($5/month).

## Bước 5: Set Stack và Buildpack

```bash
# Set stack to container (để dùng Docker)
heroku stack:set container -a cotton-broker-system

# Kiểm tra
heroku stack -a cotton-broker-system
```

## Bước 6: Khởi tạo Git (nếu chưa có)

```bash
# Kiểm tra git đã init chưa
git status

# Nếu chưa, init git
git init

# Add tất cả files
git add .

# Commit
git commit -m "Initial commit - CBAS v1.0 ready for Heroku"
```

## Bước 7: Thêm Heroku Remote

```bash
# Thêm Heroku remote
heroku git:remote -a cotton-broker-system

# Kiểm tra
git remote -v
```

Bạn sẽ thấy:
```
heroku  https://git.heroku.com/cotton-broker-system.git (fetch)
heroku  https://git.heroku.com/cotton-broker-system.git (push)
```

## Bước 8: Deploy lên Heroku

```bash
# Push code lên Heroku
git push heroku main

# Nếu branch của bạn là master
git push heroku master

# Nếu bạn đang ở branch khác
git push heroku your-branch:main
```

**Quá trình deploy sẽ:**
1. Build Docker image
2. Push image lên Heroku registry
3. Release và start dyno
4. Chạy migrations tự động

Đợi khoảng 3-5 phút.

## Bước 9: Kiểm tra Deployment

### 9.1. Xem logs
```bash
heroku logs --tail -a cotton-broker-system
```

### 9.2. Kiểm tra app status
```bash
heroku ps -a cotton-broker-system
```

### 9.3. Mở app
```bash
heroku open -a cotton-broker-system
```

Hoặc truy cập: `https://cotton-broker-system.herokuapp.com`

## Bước 10: Kiểm tra Database

```bash
# Xem database info
heroku pg:info -a cotton-broker-system

# Xem connection string
heroku config:get DATABASE_URL -a cotton-broker-system

# Connect vào database (optional)
heroku pg:psql -a cotton-broker-system
```

Trong psql:
```sql
-- Xem tables
\dt

-- Xem users
SELECT * FROM "Users";

-- Exit
\q
```

## Bước 11: Test Application

1. Truy cập app URL
2. Login với `admin` / `admin123`
3. Test upload và process offer

## Troubleshooting

### Lỗi: Application Error

```bash
# Xem logs chi tiết
heroku logs --tail -a cotton-broker-system

# Restart app
heroku restart -a cotton-broker-system
```

### Lỗi: Database connection failed

```bash
# Kiểm tra DATABASE_URL
heroku config -a cotton-broker-system

# Xem database status
heroku pg:info -a cotton-broker-system
```

### Lỗi: Build failed

```bash
# Kiểm tra Dockerfile
cat Dockerfile

# Kiểm tra heroku.yml
cat heroku.yml

# Build lại
git add .
git commit -m "Fix build"
git push heroku main
```

### Lỗi: Migrations failed

```bash
# Chạy migrations thủ công
heroku run dotnet ef database update -a cotton-broker-system

# Hoặc reset database
heroku pg:reset DATABASE -a cotton-broker-system --confirm cotton-broker-system
git push heroku main  # Deploy lại để chạy migrations
```

## Cấu hình Nâng cao

### 1. Set Environment Variables

```bash
# Set ASPNETCORE_ENVIRONMENT
heroku config:set ASPNETCORE_ENVIRONMENT=Production -a cotton-broker-system

# Set custom config
heroku config:set MAX_FILE_SIZE_MB=20 -a cotton-broker-system
```

### 2. Scale Dynos

```bash
# Xem dyno hiện tại
heroku ps -a cotton-broker-system

# Scale to 2 dynos (cần upgrade plan)
heroku ps:scale web=2 -a cotton-broker-system

# Upgrade dyno type
heroku ps:type web=standard-1x -a cotton-broker-system
```

### 3. Add Custom Domain

```bash
# Add domain
heroku domains:add www.cottonbroker.com -a cotton-broker-system

# Xem DNS targets
heroku domains -a cotton-broker-system
```

Sau đó cấu hình DNS:
- Type: CNAME
- Name: www
- Value: (DNS target từ Heroku)

### 4. Enable SSL

```bash
# SSL tự động enable cho custom domain
# Kiểm tra
heroku certs -a cotton-broker-system
```

## Backup và Restore

### Backup Database

```bash
# Tạo backup
heroku pg:backups:capture -a cotton-broker-system

# Xem backups
heroku pg:backups -a cotton-broker-system

# Download backup
heroku pg:backups:download -a cotton-broker-system
```

### Restore Database

```bash
# Restore từ backup
heroku pg:backups:restore b001 DATABASE_URL -a cotton-broker-system
```

## Monitoring

### 1. View Metrics

```bash
# Xem metrics
heroku logs --tail -a cotton-broker-system

# Xem dyno metrics (cần dashboard)
# https://dashboard.heroku.com/apps/cotton-broker-system/metrics
```

### 2. Add Logging

```bash
# Add Papertrail (log management)
heroku addons:create papertrail -a cotton-broker-system

# Xem logs
heroku addons:open papertrail -a cotton-broker-system
```

## Update Application

Khi có code mới:

```bash
# 1. Commit changes
git add .
git commit -m "Update: your changes"

# 2. Push to Heroku
git push heroku main

# 3. Xem logs
heroku logs --tail -a cotton-broker-system
```

## Cost Estimation

### Minimum Setup
- **Eco Dyno**: $5/month (550 hours)
- **Mini Postgres**: $5/month (10GB storage)
- **Total**: ~$10/month

### Recommended Setup
- **Basic Dyno**: $7/month (always on)
- **Essential Postgres**: $5/month (64GB storage)
- **Papertrail**: Free tier
- **Total**: ~$12/month

### Production Setup
- **Standard-1X Dyno**: $25/month
- **Standard-0 Postgres**: $50/month
- **Papertrail**: $7/month
- **Total**: ~$82/month

## Security Checklist

Sau khi deploy:

- [ ] Đổi password admin (hiện tại: admin/admin123)
- [ ] Set strong DATABASE_URL
- [ ] Enable SSL cho custom domain
- [ ] Set ASPNETCORE_ENVIRONMENT=Production
- [ ] Review environment variables
- [ ] Enable automatic backups
- [ ] Set up monitoring
- [ ] Review logs regularly

## Useful Commands

```bash
# Xem tất cả apps
heroku apps

# Xem config
heroku config -a cotton-broker-system

# Restart app
heroku restart -a cotton-broker-system

# Xem logs
heroku logs --tail -a cotton-broker-system

# Run command
heroku run bash -a cotton-broker-system

# Database info
heroku pg:info -a cotton-broker-system

# Scale dynos
heroku ps:scale web=1 -a cotton-broker-system

# Delete app (cẩn thận!)
heroku apps:destroy cotton-broker-system
```

## Alternative: Railway.app (Easier & Cheaper)

Nếu gặp khó khăn với Heroku, thử Railway.app:

1. Truy cập https://railway.app
2. Login với GitHub
3. Click "New Project" → "Deploy from GitHub repo"
4. Chọn repo CottonAgent
5. Add PostgreSQL database
6. Deploy tự động

**Ưu điểm Railway**:
- ✅ Dễ hơn Heroku
- ✅ Free tier tốt hơn ($5 credit/month)
- ✅ Tự động detect .NET
- ✅ GitHub integration

## Support

Nếu gặp vấn đề:
1. Xem logs: `heroku logs --tail`
2. Check status: `heroku ps`
3. Xem docs: https://devcenter.heroku.com
4. Email: support@cbas.local

---

**Chúc bạn deploy thành công! 🚀**
