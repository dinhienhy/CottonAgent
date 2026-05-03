# Hướng dẫn Deployment CBAS lên Heroku

## Yêu cầu

- Tài khoản Heroku (miễn phí hoặc trả phí)
- Heroku CLI đã cài đặt
- Git đã cài đặt
- .NET 8.0 SDK

## Bước 1: Chuẩn bị Project

### 1.1. Tạo buildpack configuration

Tạo file `heroku.yml` trong thư mục gốc:

```yaml
build:
  languages:
    - dotnet
run:
  web: dotnet CBAS.Web.dll --urls http://0.0.0.0:$PORT
```

### 1.2. Cập nhật appsettings.json

Đảm bảo connection string có thể đọc từ environment variable:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cbas_db;Username=postgres;Password=postgres"
  }
}
```

### 1.3. Cập nhật Program.cs

Connection string đã được cấu hình để đọc từ environment variable hoặc fallback về localhost.

## Bước 2: Tạo Heroku App

### 2.1. Login vào Heroku

```bash
heroku login
```

### 2.2. Tạo app mới

```bash
heroku create cotton-broker-system
```

Hoặc với tên tùy chỉnh:

```bash
heroku create your-custom-name
```

### 2.3. Thêm PostgreSQL addon

```bash
heroku addons:create heroku-postgresql:essential-0
```

Lưu ý: 
- `essential-0` là plan trả phí ($5/tháng)
- Nếu dùng free tier (đã bị Heroku ngừng), có thể dùng external PostgreSQL

### 2.4. Kiểm tra database URL

```bash
heroku config:get DATABASE_URL
```

## Bước 3: Cấu hình Environment Variables

### 3.1. Set connection string

Heroku tự động tạo `DATABASE_URL`, nhưng format khác. Cần convert:

```bash
# Lấy DATABASE_URL
heroku config:get DATABASE_URL

# Convert sang format .NET (ví dụ)
# postgres://user:password@host:5432/dbname
# => Host=host;Database=dbname;Username=user;Password=password;SSL Mode=Require;Trust Server Certificate=true

heroku config:set ConnectionStrings__DefaultConnection="Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASS;SSL Mode=Require;Trust Server Certificate=true"
```

### 3.2. Set environment

```bash
heroku config:set ASPNETCORE_ENVIRONMENT=Production
```

### 3.3. Disable HTTPS redirect (nếu cần)

```bash
heroku config:set ASPNETCORE_URLS=http://+:$PORT
```

## Bước 4: Cấu hình Buildpack

### 4.1. Set buildpack cho .NET

```bash
heroku buildpacks:set https://github.com/jincod/dotnetcore-buildpack
```

Hoặc dùng buildpack chính thức:

```bash
heroku buildpacks:set heroku/dotnet
```

## Bước 5: Deploy

### 5.1. Khởi tạo Git (nếu chưa có)

```bash
git init
git add .
git commit -m "Initial commit - CBAS v1.0"
```

### 5.2. Thêm Heroku remote

```bash
heroku git:remote -a cotton-broker-system
```

### 5.3. Push code lên Heroku

```bash
git push heroku main
```

Hoặc nếu branch là master:

```bash
git push heroku master
```

## Bước 6: Chạy Database Migration

### 6.1. Chạy migration

```bash
heroku run dotnet ef database update
```

Hoặc migration tự động chạy khi app khởi động (đã cấu hình trong Program.cs).

### 6.2. Kiểm tra logs

```bash
heroku logs --tail
```

## Bước 7: Mở App

```bash
heroku open
```

Hoặc truy cập: `https://cotton-broker-system.herokuapp.com`

## Bước 8: Cấu hình Domain (Optional)

### 8.1. Thêm custom domain

```bash
heroku domains:add www.cottonbroker.com
```

### 8.2. Cấu hình DNS

Thêm CNAME record:
- Name: `www`
- Value: `cotton-broker-system.herokuapp.com`

## Troubleshooting

### Lỗi: Application Error

Kiểm tra logs:
```bash
heroku logs --tail
```

### Lỗi: Database connection failed

Kiểm tra connection string:
```bash
heroku config
```

Đảm bảo có `SSL Mode=Require` cho Heroku Postgres.

### Lỗi: Port binding

Đảm bảo app lắng nghe trên `$PORT`:
```csharp
app.Run($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "5000"}");
```

### Lỗi: Build failed

Kiểm tra:
- .NET SDK version trong `.csproj`
- Buildpack đúng
- Dependencies trong `.csproj`

## Alternative: Deploy lên Railway.app

Railway.app dễ hơn Heroku và có free tier tốt hơn:

### 1. Tạo tài khoản Railway.app

Truy cập: https://railway.app

### 2. Tạo project mới

- Click "New Project"
- Chọn "Deploy from GitHub repo"
- Authorize GitHub và chọn repo

### 3. Thêm PostgreSQL

- Click "New" → "Database" → "PostgreSQL"
- Railway tự động tạo database

### 4. Cấu hình Variables

Railway tự động detect .NET project và set variables.

Thêm:
```
ASPNETCORE_ENVIRONMENT=Production
```

### 5. Deploy

Railway tự động deploy khi push code lên GitHub.

## Alternative: Deploy lên Azure App Service

### 1. Tạo Resource Group

```bash
az group create --name cbas-rg --location southeastasia
```

### 2. Tạo App Service Plan

```bash
az appservice plan create --name cbas-plan --resource-group cbas-rg --sku B1 --is-linux
```

### 3. Tạo Web App

```bash
az webapp create --resource-group cbas-rg --plan cbas-plan --name cotton-broker-system --runtime "DOTNET|8.0"
```

### 4. Tạo PostgreSQL

```bash
az postgres flexible-server create --resource-group cbas-rg --name cbas-db-server --location southeastasia --admin-user cbas_admin --admin-password YourPassword123! --sku-name Standard_B1ms --tier Burstable --storage-size 32
```

### 5. Deploy

```bash
az webapp deployment source config-local-git --name cotton-broker-system --resource-group cbas-rg
git remote add azure <GIT_URL>
git push azure main
```

## Backup Database

### Heroku

```bash
heroku pg:backups:capture
heroku pg:backups:download
```

### Manual backup

```bash
pg_dump -h HOST -U USER -d DATABASE > backup.sql
```

## Monitoring

### Heroku

```bash
heroku logs --tail
heroku ps
heroku pg:info
```

### Application Insights (Azure)

Thêm package:
```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

Cấu hình trong `Program.cs`:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

## Security Checklist

- [ ] Đổi default password admin/admin123
- [ ] Enable HTTPS
- [ ] Set strong connection string password
- [ ] Enable database SSL
- [ ] Set CORS policy
- [ ] Enable rate limiting
- [ ] Add authentication middleware
- [ ] Encrypt sensitive data
- [ ] Regular backup database
- [ ] Monitor logs

## Performance Optimization

### 1. Enable Response Compression

```csharp
builder.Services.AddResponseCompression();
app.UseResponseCompression();
```

### 2. Enable Response Caching

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### 3. Database Connection Pooling

Đã tự động enable với Npgsql.

### 4. Use CDN cho static files

Upload CSS/JS lên CDN như Cloudflare.

## Scaling

### Heroku

```bash
# Scale to 2 dynos
heroku ps:scale web=2

# Upgrade dyno type
heroku ps:type web=standard-2x
```

### Azure

```bash
az appservice plan update --name cbas-plan --resource-group cbas-rg --sku P1V2
```

## Cost Estimation

### Heroku
- Eco Dyno: $5/month
- Essential Postgres: $5/month
- **Total: ~$10/month**

### Railway.app
- Free tier: $5 credit/month
- After free tier: ~$5-10/month

### Azure
- App Service B1: ~$13/month
- PostgreSQL Flexible B1ms: ~$12/month
- **Total: ~$25/month**

## Support

Nếu gặp vấn đề, kiểm tra:
1. Logs: `heroku logs --tail`
2. Database: `heroku pg:info`
3. Config: `heroku config`
4. Build: `heroku builds:info`

Liên hệ: support@cbas.local
