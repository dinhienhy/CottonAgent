# Quick Start Guide - CBAS

Hướng dẫn nhanh để chạy Cotton Broker Automation System trong 5 phút.

## Bước 1: Cài đặt Prerequisites (2 phút)

### Windows

1. **Cài đặt .NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Chạy installer và làm theo hướng dẫn

2. **Cài đặt Docker Desktop** (cho PostgreSQL)
   - Download: https://www.docker.com/products/docker-desktop
   - Chạy installer và khởi động Docker

### macOS

```bash
# Cài đặt .NET 8.0
brew install dotnet-sdk

# Cài đặt Docker
brew install --cask docker
```

### Linux (Ubuntu/Debian)

```bash
# Cài đặt .NET 8.0
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Cài đặt Docker
sudo apt-get install docker.io docker-compose
```

## Bước 2: Clone và Setup (1 phút)

```bash
# Clone repository (nếu có)
git clone https://github.com/your-org/cotton-agent.git
cd cotton-agent

# Hoặc nếu đã có source code, cd vào thư mục
cd CottonAgent
```

## Bước 3: Khởi động Database (1 phút)

```bash
# Khởi động PostgreSQL bằng Docker
docker-compose up -d

# Kiểm tra PostgreSQL đã chạy
docker ps
```

Bạn sẽ thấy:
- `cbas_postgres` - PostgreSQL database
- `cbas_pgadmin` - PgAdmin web interface

## Bước 4: Chạy Application (1 phút)

```bash
# Restore packages
dotnet restore

# Chạy migration (tự động tạo tables)
dotnet ef database update

# Chạy application
dotnet run
```

Đợi cho đến khi thấy:
```
Now listening on: https://localhost:5001
```

## Bước 5: Truy cập và Sử dụng

### 5.1. Mở trình duyệt

Truy cập: **https://localhost:5001**

### 5.2. Login

- Username: `admin`
- Password: `admin123`

### 5.3. Upload và Process Offer

1. Click "Offer Processor" trong menu
2. Nhập thông tin:
   - Supplier Name: `Toyoshima`
   - ICE Value: `84.19`
   - Commission: `2.00`
3. Upload Offer PDF
4. Upload HVI Report PDFs (có thể chọn nhiều file)
5. Click "Process Offer"
6. Xem kết quả trong bảng
7. Click "Export Excel" để tải file

## Troubleshooting

### Lỗi: Port 5432 already in use

PostgreSQL đã chạy trên máy. Có 2 cách:

**Cách 1**: Dùng PostgreSQL hiện có
```bash
# Sửa appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cbas_db;Username=YOUR_USER;Password=YOUR_PASSWORD"
  }
}
```

**Cách 2**: Đổi port trong docker-compose.yml
```yaml
ports:
  - "5433:5432"  # Đổi từ 5432 sang 5433
```

### Lỗi: dotnet command not found

.NET SDK chưa được cài hoặc chưa có trong PATH.

```bash
# Kiểm tra
dotnet --version

# Nếu không có, cài lại .NET SDK
```

### Lỗi: Docker daemon not running

```bash
# Windows/Mac: Mở Docker Desktop

# Linux:
sudo systemctl start docker
```

### Lỗi: Migration failed

```bash
# Xóa database và tạo lại
docker-compose down -v
docker-compose up -d

# Chạy lại migration
dotnet ef database update
```

### Lỗi: Cannot connect to database

Kiểm tra:
```bash
# PostgreSQL đang chạy?
docker ps | grep postgres

# Connection string đúng?
cat appsettings.json
```

## Dừng Application

```bash
# Ctrl+C để dừng dotnet run

# Dừng PostgreSQL
docker-compose down

# Dừng và xóa data
docker-compose down -v
```

## Next Steps

Sau khi chạy thành công:

1. **Đọc README.md** - Hiểu đầy đủ về hệ thống
2. **Đọc TESTING.md** - Test các chức năng
3. **Đọc DEPLOYMENT.md** - Deploy lên production
4. **Customize** - Điều chỉnh theo nhu cầu

## Quick Commands Cheat Sheet

```bash
# Development
dotnet run                          # Chạy app
dotnet watch run                    # Chạy với hot reload
dotnet build                        # Build project
dotnet clean                        # Clean build artifacts

# Database
dotnet ef migrations add <Name>     # Tạo migration mới
dotnet ef database update           # Apply migrations
dotnet ef database drop             # Xóa database
dotnet ef migrations remove         # Xóa migration cuối

# Docker
docker-compose up -d                # Start services
docker-compose down                 # Stop services
docker-compose logs -f              # View logs
docker-compose ps                   # List services

# Git
git status                          # Check status
git add .                           # Stage changes
git commit -m "message"             # Commit
git push                            # Push to remote
```

## Default Credentials

### Application
- Username: `admin`
- Password: `admin123`

### PgAdmin (http://localhost:5050)
- Email: `admin@cbas.local`
- Password: `admin123`

### PostgreSQL
- Host: `localhost`
- Port: `5432`
- Database: `cbas_db`
- Username: `postgres`
- Password: `postgres`

## Support

Nếu gặp vấn đề:

1. Kiểm tra logs: `dotnet run --verbosity detailed`
2. Kiểm tra Docker: `docker-compose logs`
3. Xem TESTING.md cho troubleshooting chi tiết
4. Email: support@cbas.local

## Video Tutorial

Coming soon: Link to video tutorial

---

**Chúc bạn sử dụng CBAS thành công! 🎉**
