# Cotton Broker Automation System (CBAS)

Hệ thống Tự động Xử lý Offer Bông - Version 1.5

## Tổng quan

CBAS là hệ thống web tự động hóa quy trình nhận Offer từ nhiều shipper (Toyoshima, Olam, Brighann) và tạo ra Output chuẩn để chào nhà máy Việt Nam.

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

Hệ thống hỗ trợ 2 loại giá:

### 1. On-Call (Basis + ICE)
Basis trong Offer là **cents** (không phải points). Ví dụ: `11.00` = 11.00 c/lb.

```
Outright (c/lb) = ICE Settlement + Basis (cents)
Giá (c/kg) = Outright (c/lb) × 2.20462
Giá có Commission = Giá (c/kg) - Commission
```

**Ví dụ** (ME066M6 - Toyoshima):
```
Outright = 84.19 + 11.00 = 95.19 c/lb
Giá (c/kg) = 95.19 × 2.20462 = 209.86 c/kg
Giá net = 209.86 - 2.00 = 207.86 c/kg
```

### 2. Fixed Price (OutrightPrice)
Khi shipper báo giá cố định (ví dụ: Olam afloat, Brighann CIF):

```
Giá (c/kg) = OutrightPrice (c/lb) × 2.20462
Giá có Commission = Giá (c/kg) - Commission
```

**Ưu tiên**: Nếu `OutrightPrice > 0` → dùng OutrightPrice. Ngược lại → dùng ICE + BasisCents.

## Quản lý Shipper / Lot (Phase 2A)

### Shippers (`/shippers`)
- Thêm/sửa/bật/tắt shipper
- Khi upload Offer, hệ thống tự tạo Shipper nếu chưa tồn tại

### Offers (`/offers`)
- Xem lịch sử tất cả offers
- Filter theo shipper, ngày
- **Xoá offer**: nút xoá với xác nhận, cascade xoá OfferLots + ProcessedOutputs, gỡ liên kết Lot

### Lots (`/lots`) — Quản lý Lot nâng cao (v1.4)
- **20 cột**: Lot Code, Shipper, Origin, CropYear, Type/Spec, Shipment/ETA, QTY Orig, QTY Avail, Mic, Len, GPT, Basis, Giá c/kg, +Comm, Net, Status, HVI, Action
- **ICE + Commission** inputs ở góc trên — thay đổi → giá realtime refresh
- **Multi-select** + nút "Tạo Output Chào Hàng" → export Excel (nhóm theo Shipment Date)
- **HVI detail modal**: click "HVI ✓" → xem/chỉnh Mic, Length, Str, Uniformity, Color, Leaf
- **Xóa lot**: nút "Xóa" trong cột Action
- Filter: shipper, origin, status, lot code, **shipment month**
- Summary cards: Available / Reserved / Sold / Tổng QTY
- Thay đổi status: Reserve, Sold, Reopen
- **So sánh PDF ↔ Lots**: chọn Offer → hiển thị raw PDF text bên trái, bảng lots bên phải, click lot → highlight chính xác dòng gốc trong PDF
- Auto-sync: khi upload Offer mới, **mỗi OfferLot tạo 1 Lot riêng** (1:1, không merge trùng LotCode)
- Auto-create HVIReport từ spec data (Mic/Len/GPT) khi parse Offer

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

### Bảng Lots (Phase 2A + v1.4)
- Id (PK), LotCode (Unique), ShipperId (FK → Shippers)
- Origin, CropYear, Type
- QuantityOriginal, QuantityAvailable
- BasisCents (decimal, 11.00 = hiển thị 1100 points)
- **OutrightPrice** (decimal, giá cố định c/lb, ưu tiên khi > 0)
- ShipmentDate, ShipmentDateText, SpecialSpec
- Status (Available/Reserved/Sold)
- LatestOfferId (FK → Offers), HVIReportId (FK → HVIReports)
- CreatedAt, UpdatedAt

### Bảng Offers (cập nhật v1.5)
- RawPdfText (text, nullable) — lưu full text PDF gốc để so sánh

### Bảng OfferLots (cập nhật v1.5)
- LotId (PK), OfferId (FK)
- LotCode (nullable - generic lines không có lot code)
- MasterLotId (FK → Lots, nullable)
- Origin, CropYear, Quantity, QuantityText, Type
- BasisCents (basis tính bằng cents, không phải points)
- OutrightPrice, SettlementMonth, ShipmentDateText
- ColorSpec, LeafSpec, LengthSpec, MicronaireSpec, StrengthSpec
- PriceCentsPerLb
- **SourceLineNumber** (int?, nullable) — dòng gốc trong PDF raw text
- **SourceRawLine** (text, nullable) — nội dung dòng gốc để debug

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

## Multi-Shipper Parser (v1.5)

Hệ thống sử dụng **Strategy Pattern** (`IShipperParser`) để parse PDF từ nhiều shipper.

### Chiến lược parse (Hybrid)

1. **Rule-based parser ưu tiên** — nếu `CanParse()` match shipper đã biết → dùng regex parser (deterministic, luôn cho kết quả giống nhau)
2. **AI parser (Claude) chỉ cho shipper unknown** — khi không có rule-based parser nào match
3. AI config: `temperature=0` (greedy decoding), prompt caching, instruction nhất quán
4. Mỗi OfferLot lưu `SourceLineNumber` + `SourceRawLine` để highlight chính xác dòng gốc trong PDF

### Parsers hiện có

| Parser | File | CanParse trigger | Đặc điểm |
|--------|------|------------------|----------|
| **ToyoshimaParser** | `Services/Parsers/ToyoshimaParser.cs` | Filename chứa "Toyoshima" / "Offer_FE", hoặc nội dung chứa "Toyoshima"/"Nishiki" | Origin sections, compressed spec (SM37G5), M/E Recap lots |
| **OlamParser** | `Services/Parsers/OlamParser.cs` | Filename/content chứa "Olam" | 6 patterns: Afloat, Recap+Avg, US Recap, Named lot, Generic on-call, Fixed+basis |
| **BrighannParser** | `Services/Parsers/BrighannParser.cs` | Filename/content chứa "Brighann" | CIF format, auto-generate LotCode (AU-SM37-001) |

### Thêm shipper mới

1. Tạo class implement `IShipperParser` trong `Services/Parsers/`
2. Implement `ShipperName`, `CanParse()`, `Parse()`
3. Register trong `PdfParserService.cs` constructor

### LotCode generation

- **Toyoshima**: Lot code từ PDF (ME066M6, ME096A1...)
- **Olam**: Lot name nếu có (T/APEX), hoặc auto-gen `OL-{origin}-{seq}`
- **Brighann**: Auto-gen `{originCode}-{type}{staple}-{seq}` (BR-SM37-001)

## Lịch sử Version

### v1.5.0 (2026-05-14)
- **Parser priority**: rule-based parser chạy trước AI → deterministic, cùng file luôn ra cùng kết quả
- **AI temperature=0**: Claude API set temperature=0 + instruction nhất quán cho shipper chưa có parser
- **Source line tracking**: mỗi OfferLot lưu `SourceLineNumber` + `SourceRawLine` khi parse
- **So sánh PDF ↔ Lots**: panel side-by-side trên `/lots` và `/offer-processor`, click lot → highlight chính xác 1 dòng
- **Xoá offer**: nút xoá trong `/offers` với cascade delete (OfferLots, ProcessedOutputs, gỡ Lot.LatestOfferId)
- **1:1 Lot mapping**: mỗi OfferLot tạo 1 Lot riêng, không merge trùng LotCode
- **RawPdfText**: lưu full PDF text trên Offer để so sánh sau
- Migration: `AddOfferRawPdfText` + `AddOfferLotSourceTracking`

### v1.4.0 (2026-05-04)
- **Multi-shipper parser**: Olam (6 patterns), Brighann (CIF format)
- **OutrightPrice**: hỗ trợ Fixed Price cho lots không dùng ICE+Basis
- **Auto-create HVIReport** từ parsed spec (Mic/Len/GPT) khi sync lots
- **Lot page cải tiến**: gộp Type/Spec, Shipment/ETA, hiển thị Mic/Len/GPT trực tiếp
- **Xóa lot**: nút xóa trong Lot Management
- Migration: `AddLotOutrightPrice` thêm cột `OutrightPrice` vào bảng `Lots`

### v1.3.0 (2026-05-04)
- Lot Management page 23 cột
- Multi-select export Excel
- HVI detail modal
- Filter theo shipper, origin, status, shipment month

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
- Thêm parser cho shipper mới (Cargill, Louis Dreyfus...)
- API cho mobile app
- Batch re-parse offers khi cập nhật parser

## Liên hệ

Agent Bông Việt Nam
Email: support@cottonagent.vn
