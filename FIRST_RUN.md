# 🚀 First Run Instructions - CBAS

## Chạy lần đầu tiên (First Time Setup)

### Bước 1: Khởi động PostgreSQL

```bash
# Mở terminal/command prompt tại thư mục project
cd c:/Users/dinhi/Dropbox/Workspace/CascadeProjects/CottonAgent

# Khởi động PostgreSQL và PgAdmin
docker-compose up -d

# Kiểm tra services đã chạy
docker ps
```

Bạn sẽ thấy 2 containers:
- `cbas_postgres` - PostgreSQL database (port 5432)
- `cbas_pgadmin` - PgAdmin web UI (port 5050)

### Bước 2: Chạy Application

```bash
# Restore packages (chỉ cần lần đầu)
dotnet restore

# Chạy application
dotnet run
```

Application sẽ:
1. Tự động chạy database migrations
2. Tạo tables trong PostgreSQL
3. Tạo default admin user
4. Khởi động web server

Đợi cho đến khi thấy:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

### Bước 3: Truy cập Application

1. Mở trình duyệt
2. Truy cập: **https://localhost:5001**
3. Chấp nhận self-signed certificate warning (nếu có)

### Bước 4: Login

Tại trang login:
- **Username**: `admin`
- **Password**: `admin123`

Click "Login"

### Bước 5: Sử dụng

1. Click "Offer Processor" trong menu
2. Điền thông tin:
   - Supplier Name: `Toyoshima`
   - ICE Value: `84.19`
   - Commission: `2.00`
3. Upload Offer PDF (nếu có)
4. Upload HVI PDF files (nếu có)
5. Click "Process Offer"
6. Xem kết quả và Export Excel

## 🔍 Kiểm tra Database (Optional)

### Sử dụng PgAdmin

1. Mở trình duyệt
2. Truy cập: **http://localhost:5050**
3. Login PgAdmin:
   - Email: `admin@cbas.local`
   - Password: `admin123`
4. Add Server:
   - Name: `CBAS Local`
   - Host: `postgres` (tên container)
   - Port: `5432`
   - Database: `cbas_db`
   - Username: `postgres`
   - Password: `postgres`

### Kiểm tra Tables

Sau khi connect, bạn sẽ thấy các tables:
- `Offers`
- `OfferLots`
- `HVIReports`
- `ProcessedOutputs`
- `Users`
- `__EFMigrationsHistory`

## 🛑 Dừng Application

### Dừng Web App
- Nhấn `Ctrl+C` trong terminal đang chạy `dotnet run`

### Dừng PostgreSQL
```bash
# Dừng containers nhưng giữ data
docker-compose stop

# Dừng và xóa containers (giữ data)
docker-compose down

# Dừng và xóa tất cả (bao gồm data)
docker-compose down -v
```

## 🔄 Chạy lại lần sau

Các lần sau chỉ cần:

```bash
# 1. Start PostgreSQL (nếu đã stop)
docker-compose up -d

# 2. Run application
dotnet run
```

## ⚠️ Troubleshooting

### Lỗi: "Port 5432 is already in use"

PostgreSQL đã chạy trên máy. Giải pháp:

**Option 1**: Dùng PostgreSQL hiện có
- Sửa `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cbas_db;Username=YOUR_USER;Password=YOUR_PASS"
  }
}
```

**Option 2**: Đổi port Docker
- Sửa `docker-compose.yml`:
```yaml
ports:
  - "5433:5432"  # Đổi port external
```
- Sửa `appsettings.json`:
```json
"DefaultConnection": "Host=localhost;Port=5433;Database=cbas_db;..."
```

### Lỗi: "Unable to connect to database"

Kiểm tra:
```bash
# PostgreSQL có chạy không?
docker ps | grep postgres

# Nếu không, start lại
docker-compose up -d postgres
```

### Lỗi: "Migration failed"

Reset database:
```bash
# Xóa database và tạo lại
docker-compose down -v
docker-compose up -d

# Chạy lại app (migration tự động)
dotnet run
```

### Lỗi: "dotnet command not found"

.NET SDK chưa cài hoặc chưa trong PATH.

```bash
# Kiểm tra
dotnet --version

# Nếu không có output, cài .NET 8.0 SDK
# Download: https://dotnet.microsoft.com/download/dotnet/8.0
```

### Lỗi: "Docker daemon is not running"

```bash
# Windows/Mac: Mở Docker Desktop application

# Linux:
sudo systemctl start docker
```

## 📝 Lưu ý quan trọng

### 1. Đổi Password Admin
Sau khi login lần đầu, nên đổi password mặc định:
- Hiện tại chưa có UI để đổi password
- Có thể đổi trực tiếp trong database qua PgAdmin
- Hoặc đợi update version tiếp theo

### 2. Backup Data
Nếu muốn backup data:
```bash
# Export database
docker exec cbas_postgres pg_dump -U postgres cbas_db > backup.sql

# Import lại
docker exec -i cbas_postgres psql -U postgres cbas_db < backup.sql
```

### 3. Production Deployment
Khi deploy production:
- ⚠️ Đổi password admin
- ⚠️ Đổi PostgreSQL password
- ⚠️ Enable HTTPS
- ⚠️ Set strong connection string
- ⚠️ Đọc DEPLOYMENT.md

## 📚 Tài liệu tham khảo

- **QUICKSTART.md** - Hướng dẫn nhanh 5 phút
- **README.md** - Tài liệu đầy đủ
- **TESTING.md** - Hướng dẫn test
- **DEPLOYMENT.md** - Hướng dẫn deploy
- **PROJECT_SUMMARY.md** - Tổng quan dự án

## ✅ Checklist lần đầu

- [ ] Docker Desktop đã cài và đang chạy
- [ ] .NET 8.0 SDK đã cài
- [ ] `docker-compose up -d` thành công
- [ ] `dotnet run` thành công
- [ ] Truy cập https://localhost:5001 được
- [ ] Login với admin/admin123 thành công
- [ ] Thấy trang Offer Processor

## 🎉 Chúc mừng!

Nếu tất cả các bước trên thành công, bạn đã sẵn sàng sử dụng CBAS!

Để test đầy đủ, cần:
- File Offer PDF mẫu từ Toyoshima
- File HVI Report PDF mẫu

Liên hệ support@cbas.local nếu cần hỗ trợ.
