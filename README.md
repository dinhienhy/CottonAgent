# Cotton Broker Automation System (CBAS)

Hệ thống Tự động Xử lý Offer Bông - Version 1.0

## Tổng quan

CBAS là hệ thống web tự động hóa quy trình nhận Offer từ shipper (Toyoshima và các supplier khác) và tạo ra Output chuẩn để chào nhà máy Việt Nam.

## Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0
- **Frontend**: Blazor Server
- **Database**: PostgreSQL với Entity Framework Core
- **PDF Processing**: PdfPig
- **Excel Export**: ClosedXML

## Yêu cầu hệ thống

- .NET 8.0 SDK
- PostgreSQL 12 trở lên
- Windows/Linux/macOS

## Cài đặt

### 1. Cài đặt PostgreSQL

Tải và cài đặt PostgreSQL từ: https://www.postgresql.org/download/

Tạo database:
```sql
CREATE DATABASE cbas_db;
```

### 2. Cấu hình Connection String

Cập nhật file `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cbas_db;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 3. Chạy Migration

```bash
dotnet ef database update
```

### 4. Chạy ứng dụng

```bash
dotnet run
```

Truy cập: https://localhost:5001

## Hướng dẫn sử dụng

### Bước 1: Upload Files

1. Truy cập trang "Offer Processor"
2. Nhập thông tin:
   - Supplier Name (mặc định: Toyoshima)
   - ICE Value (mặc định: 84.19)
   - Commission % (mặc định: 2.00)
3. Upload 1 file Offer PDF
4. Upload nhiều file HVI Report PDF (tối đa 20 files)

### Bước 2: Xử lý

- Click "Process Offer"
- Hệ thống sẽ:
  - Parse Offer PDF để trích xuất thông tin các lô
  - Parse từng HVI PDF để lấy chỉ số kỹ thuật
  - Tự động liên kết HVI với lô dựa trên Lot Code
  - Tính giá theo công thức: (ICE + Basis/100) × 2.20462
  - Áp dụng Commission

### Bước 3: Xuất Excel

- Xem kết quả trên bảng
- Click "Export Excel" để tải file .xlsx
- File Excel có format chuẩn, sẵn sàng copy-paste

## Công thức tính giá

```
Giá (c/lb) = ICE + (Basis points / 100)
Giá (c/kg) = Giá (c/lb) × 2.20462
Giá có Commission = Giá (c/kg) - Commission
Giá net = Giá có Commission
```

## Cấu trúc Database

### Bảng Offers
- OfferId (PK)
- OfferDate
- SupplierName
- FileName
- ICEValue
- CommissionPercent
- CreatedAt

### Bảng OfferLots
- LotId (PK)
- OfferId (FK)
- LotCode (ví dụ: ME066M6)
- Origin
- CropYear
- Quantity
- Type
- SpecialSpec
- BasisPoints
- ShipmentDate
- PriceCentsPerLb

### Bảng HVIReports
- HVIId (PK)
- LotCode (Unique)
- FileName
- Micronaire
- Length
- StrengthGPT
- Uniformity
- ColorRd
- ColorGrade
- Leaf
- CropYear
- TotalBales
- RawDataJson

### Bảng ProcessedOutputs
- OutputId (PK)
- OfferId (FK)
- LotId (FK)
- STT
- Origin, CropYear, Quantity, Type...
- PriceCentsPerKg
- PriceWithCommission
- NetPrice
- Notes

## Format Output Excel

Các cột (theo thứ tự):
1. STT
2. origin
3. crop year - vụ mùa
4. số lượng
5. loại bông
6. chỉ tiêu đặc biệt
7. Màu sắc
8. tạp lá
9. chiều dài
10. Micronaire
11. cường lực Min
12. Basis
13. Shipment Date
14. giá (c/kg)
15. giá có Commission
16. giá net (Toyoshima)
17. Ghi chú

Dữ liệu được nhóm theo Shipment Date.

## Deployment lên Heroku

### 1. Chuẩn bị

Tạo file `Procfile`:
```
web: dotnet CBAS.Web.dll
```

### 2. Tạo Heroku App

```bash
heroku create cotton-agent-app
heroku addons:create heroku-postgresql:mini
```

### 3. Cấu hình

```bash
heroku config:set ASPNETCORE_ENVIRONMENT=Production
```

### 4. Deploy

```bash
git push heroku main
```

### 5. Chạy Migration

```bash
heroku run dotnet ef database update
```

## Troubleshooting

### Lỗi kết nối PostgreSQL

Kiểm tra:
- PostgreSQL service đang chạy
- Connection string đúng
- Firewall cho phép kết nối

### Lỗi parse PDF

- Đảm bảo PDF không bị mã hóa
- Kiểm tra format PDF có đúng mẫu
- Xem log để debug regex pattern

### Lỗi upload file

- Kiểm tra file size < 10MB
- Đảm bảo file là PDF hợp lệ

## Phát triển tiếp

Giai đoạn 2 sẽ bao gồm:
- Matching hai chiều với Bid từ nhà máy
- Authentication nâng cao
- Quản lý lịch sử offers
- Dashboard thống kê
- API cho mobile app

## Liên hệ

Agent Bông Việt Nam
Email: support@cottonagent.vn
