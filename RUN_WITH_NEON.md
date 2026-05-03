# Chạy CBAS với Neon.tech Database (Không cần Docker)

## Bước 1: Tạo Database trên Neon.tech

1. Truy cập: https://neon.tech
2. Click **"Sign Up"** (miễn phí, không cần credit card)
3. Login với GitHub hoặc Email
4. Click **"Create a project"**
5. Tên project: `cbas-db`
6. Region: Chọn gần nhất (Singapore)
7. Click **"Create project"**

## Bước 2: Lấy Connection String

1. Trong project dashboard
2. Tab **"Connection Details"**
3. Copy **"Connection string"**
   - Format: `postgresql://user:pass@host/dbname?sslmode=require`

## Bước 3: Chạy App với Neon Database

### Windows PowerShell:
```powershell
# Set environment variable (THAY YOUR_CONNECTION_STRING)
$env:DATABASE_URL="postgresql://user:pass@ep-xxx.us-east-2.aws.neon.tech/neondb?sslmode=require"

# Chạy app
dotnet run
```

### Hoặc tạo file run-neon.ps1:
```powershell
# Tạo file
@"
`$env:DATABASE_URL="YOUR_CONNECTION_STRING_HERE"
dotnet run
"@ | Out-File -FilePath run-neon.ps1

# Chạy
.\run-neon.ps1
```

## Bước 4: Mở Browser

Truy cập: https://localhost:5001

Login:
- Username: `admin`
- Password: `admin123`

## Ưu điểm Neon.tech

- ✅ Miễn phí (0.5GB storage)
- ✅ Không cần credit card
- ✅ PostgreSQL 15
- ✅ Auto-scaling
- ✅ Backup tự động
- ✅ Không cần Docker
- ✅ Truy cập từ mọi nơi

## Nếu muốn dùng lại Docker sau này

Khi Docker đã fix:
```powershell
# Xóa environment variable
Remove-Item Env:\DATABASE_URL

# Chạy với Docker
docker-compose up -d
dotnet run
```

---

**Neon.tech là giải pháp tốt nhất khi Docker gặp vấn đề!** 🚀
