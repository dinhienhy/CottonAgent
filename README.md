# Cotton Broker Automation System (CBAS)

Hệ thống Tự động Xử lý Offer Bông - Version 1.3

## Tổng quan

CBAS là hệ thống web tự động hóa quy trình nhận Offer từ shipper (Toyoshima và các supplier khác) và tạo ra Output chuẩn để chào nhà máy Việt Nam.

**Live**: [cottonagent-production-92e0.up.railway.app](https://cottonagent-production-92e0.up.railway.app)

## Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0
- **Frontend**: Blazor Server
- **Database**: PostgreSQL với Entity Framework Core
- **PDF Processing**: PdfPig (word-position grouping)
- **OCR**: Tesseract CLI (cho HVI scan PDFs)
- **Excel Export**: ClosedXML
- **Deployment**: Docker + Railway (auto-deploy từ GitHub)

## Yêu cầu hệ thống

- .NET 8.0 SDK
- PostgreSQL 12 trở lên
- Tesseract OCR (tự động cài trong Docker, tùy chọn cho local dev)
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

1. Login (admin / admin123)
2. Truy cập trang "Offer Processor"
3. Nhập thông tin:
   - Chọn Shipper từ dropdown (cần tạo trước tại `/shippers`)
   - ICE Value (mặc định: 84.19)
   - Commission % (mặc định: 2.00)
4. Upload 1 file Offer PDF
5. Upload nhiều file HVI Report PDF (tối đa 20 files)
6. Click "Phân tích Offer →"

### Bước 2: HVI Review (v1.1 mới)

- Hệ thống tự động chạy **OCR** (Tesseract) trên các HVI PDF scan
- Xem kết quả OCR với confidence score và raw text
- **Kiểm tra và chỉnh sửa** dữ liệu OCR trước khi lưu
- Nếu OCR không tốt, chọn "Nhập tay" để nhập từ đầu
- Các trường HVI: Mic, Length, Strength, Uniformity, Color Rd, Leaf, CropYear, TotalBales
- Click "Lưu HVI & Tính toán →"

### Bước 3: Kết quả & Xuất Excel

- Xem bảng Output với giá đã tính
- Click "Export Excel" để tải file .xlsx
- File Excel có format chuẩn, sẵn sàng copy-paste

## Công thức tính giá

Basis trong Offer là **cents** (không phải points). Ví dụ: `11.00` = 11.00 c/lb.

```
Outright (c/lb) = ICE Settlement + Basis (cents)
Giá (c/kg) = Outright (c/lb) × 2.20462
Giá có Commission = Giá (c/kg) - Commission
```

**Ví dụ** (ME066M6):
```
Outright = 84.19 + 11.00 = 95.19 c/lb
Giá (c/kg) = 95.19 × 2.20462 = 209.86 c/kg
Giá net = 209.86 - 2.00 = 207.86 c/kg
```

## Quản lý Shipper / Lot (Phase 2A)

### Shippers (`/shippers`)
- Thêm/sửa/bật/tắt shipper
- Khi upload Offer, hệ thống tự tạo Shipper nếu chưa tồn tại

### Offers (`/offers`)
- Xem lịch sử tất cả offers
- Filter theo shipper, ngày

### Lots (`/lots`) — Quản lý Lot nâng cao (v1.3)
- **23 cột đầy đủ**: Lot Code, Shipper, Origin, CropYear, Type, Spec, Shipment, QTY, Color, Leaf, Length, Mic, Str, Basis (points), c/kg, +Comm, Net, Status, HVI, Action
- **ICE + Commission** inputs ở góc trên — thay đổi → giá realtime refresh
- **Multi-select** + nút "Tạo Output Chào Hàng" → export Excel (nhóm theo Shipment Date)
- **HVI detail modal**: click "HVI ✓" → xem/chỉnh Mic, Length, Str, Uniformity, Color, Leaf
- Filter: shipper, origin, status, lot code, **shipment month**
- Summary cards: Available / Reserved / Sold / Tổng QTY
- Thay đổi status: Reserve, Sold, Reopen
- Auto-sync: khi upload Offer mới, tự tạo/update Lot + Basis + Shipment

## Cấu trúc Database

### Bảng Shippers (Phase 2A)
- ShipperId (PK), Name (Unique)
- ContactInfo, Email, Phone, Notes, IsActive, CreatedAt

### Bảng Offers
- OfferId (PK)
- OfferDate, SupplierName, FileName
- ShipperId (FK → Shippers, nullable)
- ICEValue, CommissionPercent
- ICESettlementsJson (JSON string chứa các ICE settlement months)
- CreatedAt

### Bảng Lots (Phase 2A + v1.3)
- Id (PK), LotCode (Unique), ShipperId (FK → Shippers)
- Origin, CropYear, Type
- QuantityOriginal, QuantityAvailable
- BasisCents (decimal, 11.00 = hiển thị 1100 points)
- ShipmentDate, ShipmentDateText, SpecialSpec
- Status (Available/Reserved/Sold)
- LatestOfferId (FK → Offers), HVIReportId (FK → HVIReports)
- CreatedAt, UpdatedAt

### Bảng OfferLots
- LotId (PK), OfferId (FK)
- LotCode (nullable - generic lines không có lot code)
- MasterLotId (FK → Lots, nullable)
- Origin, CropYear, Quantity, QuantityText, Type
- BasisCents (basis tính bằng cents, không phải points)
- OutrightPrice, SettlementMonth, ShipmentDateText
- ColorSpec, LeafSpec, LengthSpec, MicronaireSpec, StrengthSpec
- PriceCentsPerLb

### Bảng HVIReports
- HVIId (PK), LotCode (Unique), MasterLotId (FK → Lots, nullable), FileName
- Micronaire, Length, StrengthGPT, Uniformity
- ColorRd, ColorGrade, Leaf
- CropYear, TotalBales, RawDataJson, CreatedAt

### Bảng ProcessedOutputs
- OutputId (PK), OfferId (FK), LotId (FK)
- STT, Origin, CropYear, Quantity, QuantityText, Type
- Color, Leaf, Length, Micronaire, StrengthMin
- MicronaireText, StrengthText, ShipmentDateText
- Basis, ShipmentDate
- PriceCentsPerKg, PriceWithCommission, NetPrice, Notes

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

## Deployment lên Railway (Production)

Project đang deploy tự động qua GitHub:

1. Push code lên `main` branch
2. Railway tự động build Docker image và deploy
3. Database migration chạy tự động khi app start

**URL**: `https://cottonagent-production-92e0.up.railway.app`

### Dockerfile
- Multi-stage build (SDK → publish → runtime)
- Runtime cài `tesseract-ocr` + `tesseract-ocr-eng` cho OCR
- Non-root user cho security

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

## Lịch sử Version

### v1.1.0 (2026-05-04)
- Tesseract OCR integration (CLI mode) cho HVI scan PDFs
- 3-step workflow: Upload → HVI Review/Edit → Results
- Parser viết lại cho format Toyoshima thật (word-position grouping)
- ICE settlements extraction từ offer PDF
- Basis lưu bằng cents (BasisCents), không phải points
- Deploy lên Railway với Docker

### v1.0.0 (2026-05-03)
- Release đầu tiên - Core functionality

## Phát triển tiếp

Giai đoạn tiếp theo:
- Matching hai chiều với Bid từ nhà máy
- Dashboard thống kê
- Multi-supplier PDF templates
- API cho mobile app

## Liên hệ

Agent Bông Việt Nam
Email: support@cottonagent.vn
