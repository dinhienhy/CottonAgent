# Cotton Broker Automation System (CBAS) - Project Summary

**Version**: 1.2.0  
**Date**: 04/05/2026  
**Status**: ✅ PRODUCTION - Deployed on Railway  
**URL**: [cottonagent-production-92e0.up.railway.app](https://cottonagent-production-92e0.up.railway.app)

---

## 📋 Tổng quan Dự án

Hệ thống CBAS tự động hóa quy trình nhận Offer từ shipper và tạo Output chuẩn để chào nhà máy Việt Nam. Bao gồm OCR tự động cho HVI scan PDFs, 3-step workflow (Upload → HVI Review → Results), và quản lý Shipper/Lot inventory.

## ✅ Các Chức năng Đã Hoàn thành

### 1. Core Features
- ✅ Upload Offer PDF từ shipper (Toyoshima)
- ✅ Upload nhiều HVI Report PDF (tối đa 20 files cùng lúc)
- ✅ Parse Offer PDF bằng **word-position grouping** (Y-coordinate rows)
- ✅ Hỗ trợ generic lines và M/E Recap lines, multiple origins
- ✅ ICE settlements extraction từ offer PDF
- ✅ **Tesseract OCR** (CLI mode) cho HVI scan PDFs
- ✅ 3-step workflow: Upload → HVI Review/Edit → Results
- ✅ Tính giá: Outright(c/lb) = ICE + BasisCents → × 2.20462 = c/kg
- ✅ Áp dụng Commission
- ✅ Export Excel (.xlsx) format chuẩn

### 1b. Phase 2A — Shipper/Lot Management
- ✅ `Shipper` entity quản lý nhà cung cấp (CRUD, Active/Inactive)
- ✅ `Lot` master entity quản lý lô hàng (QuantityAvailable, Status: Available/Reserved/Sold)
- ✅ Auto-sync: upload Offer → tự tạo Shipper + Lot, update quantity
- ✅ `/shippers` page — CRUD shipper
- ✅ `/offers` page — lịch sử offers, filter theo shipper/ngày
- ✅ `/lots` page — inventory lots, filter, summary cards, thay đổi status
- ✅ Offer Processor: dropdown chọn Shipper thay vì nhập tay

### 2. Technical Implementation

#### Backend
- ✅ ASP.NET Core 8.0 Web Application
- ✅ Blazor Server cho real-time UI
- ✅ Entity Framework Core 8.0
- ✅ PostgreSQL Database
- ✅ Repository Pattern
- ✅ Dependency Injection

#### PDF Processing
- ✅ PdfPig library cho PDF parsing (word-position grouping)
- ✅ Spec code parsing: GC31336 = Gold Color, Rd=31, Leaf=3, Staple=36
- ✅ Tesseract OCR CLI cho HVI scan PDFs (extract PNG → temp file → CLI)
- ✅ Error handling với graceful degradation (OCR fail → manual input)

#### Excel Export
- ✅ ClosedXML library
- ✅ Format chuẩn với 17 cột
- ✅ Styling và formatting
- ✅ Merged cells cho group headers

#### Database
- ✅ 5 tables: Offers, OfferLots, HVIReports, ProcessedOutputs, Users
- ✅ Foreign key relationships
- ✅ Indexes cho performance
- ✅ Migrations configured

#### Authentication
- ✅ Simple username/password login
- ✅ SHA256 password hashing
- ✅ Session management
- ✅ Default admin user

### 3. User Interface
- ✅ Responsive design với Bootstrap 5
- ✅ Modern và clean interface
- ✅ Upload progress indicators
- ✅ Error messages và validation
- ✅ Loading states
- ✅ Mobile-friendly

## 📁 Cấu trúc Project

```
CottonAgent/
├── Data/
│   └── ApplicationDbContext.cs          # EF Core DbContext
├── Models/
│   ├── Offer.cs                         # Offer entity (+ ICESettlementsJson)
│   ├── OfferLot.cs                      # Lot entity (BasisCents, spec fields)
│   ├── HVIReport.cs                     # HVI entity
│   ├── ProcessedOutput.cs               # Output entity
│   └── User.cs                          # User entity
├── Services/
│   ├── IPdfParserService.cs             # PDF parser interface
│   ├── PdfParserService.cs              # Word-position PDF parser
│   ├── IOcrService.cs                   # OCR service interface
│   ├── TesseractOcrService.cs           # Tesseract CLI OCR
│   ├── IOfferProcessingService.cs       # Business logic interface
│   ├── OfferProcessingService.cs        # Business logic + OCR integration
│   ├── IExcelExportService.cs           # Excel export interface
│   ├── ExcelExportService.cs            # Excel export implementation
│   ├── IAuthService.cs                  # Auth interface
│   └── AuthService.cs                   # Auth implementation
├── DTOs/
│   ├── OfferUploadDto.cs                # Upload data transfer object
│   ├── OutputRowDto.cs                  # Output data transfer object
│   ├── HVIInputDto.cs                   # HVI review/edit DTO (+ OcrStatus)
│   └── OcrResult.cs                     # OCR result DTO
├── Pages/
│   ├── Index.razor                      # Home page
│   ├── Login.razor                      # Login page
│   ├── OfferProcessor.razor             # 3-step workflow page
│   ├── Shippers.razor                   # CRUD quản lý Shipper
│   ├── OfferHistory.razor               # Lịch sử Offers
│   ├── LotList.razor                    # Inventory Lots
│   ├── _Host.cshtml                     # Host page
│   └── _Layout.cshtml                   # Layout
├── Shared/
│   ├── NavMenu.razor                    # Navigation menu
│   └── MainLayout.razor                 # Main layout
├── Migrations/                          # EF Core migrations
├── SampleData/Toyoshima/                # Sample PDFs for testing
├── wwwroot/                             # Static files
├── Program.cs                           # App entry + DI config
├── Dockerfile                           # Multi-stage Docker build
├── CBAS.Web.csproj                      # Project file
└── docker-compose.yml                   # Local dev setup
```

## 🗄️ Database Schema

### Shippers Table (Phase 2A)
```sql
- ShipperId (PK)
- Name (Unique), ContactInfo, Email, Phone
- Notes, IsActive, CreatedAt
```

### Offers Table
```sql
- OfferId (PK)
- OfferDate, SupplierName, FileName
- ShipperId (FK → Shippers, nullable)
- ICEValue (decimal), CommissionPercent (decimal)
- ICESettlementsJson (JSON string chứa ICE settlement months)
- CreatedAt
```

### Lots Table (Phase 2A)
```sql
- Id (PK)
- LotCode (Unique), ShipperId (FK → Shippers)
- Origin, CropYear, Type
- QuantityOriginal, QuantityAvailable
- Status (Available/Reserved/Sold)
- LatestOfferId (FK → Offers), HVIReportId (FK → HVIReports)
- CreatedAt, UpdatedAt
```

### OfferLots Table
```sql
- LotId (PK)
- OfferId (FK)
- LotCode (nullable - generic lines không có lot code)
- MasterLotId (FK → Lots, nullable)
- Origin, CropYear, Quantity, QuantityText, Type
- BasisCents (basis tính bằng cents)
- OutrightPrice, SettlementMonth, ShipmentDateText
- ColorSpec, LeafSpec, LengthSpec, MicronaireSpec, StrengthSpec
- PriceCentsPerLb
```

### HVIReports Table
```sql
- HVIId (PK)
- LotCode (Unique)
- MasterLotId (FK → Lots, nullable)
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
- CreatedAt
```

### ProcessedOutputs Table
```sql
- OutputId (PK)
- OfferId (FK), LotId (FK)
- STT
- Origin, CropYear, Quantity, QuantityText, Type
- Color, Leaf, Length, Micronaire, StrengthMin
- MicronaireText, StrengthText, ShipmentDateText
- Basis, ShipmentDate
- PriceCentsPerKg, PriceWithCommission, NetPrice
- Notes, CreatedAt
```

### Users Table
```sql
- UserId (PK)
- Username (Unique)
- PasswordHash
- FullName
- Email
- IsActive
- CreatedAt
- LastLoginAt
```

## 🔧 Technologies Used

| Category | Technology | Version |
|----------|-----------|--------|
| Framework | ASP.NET Core | 8.0 |
| UI | Blazor Server | 8.0 |
| Database | PostgreSQL | 15+ |
| ORM | Entity Framework Core | 8.0.4 |
| PDF Processing | PdfPig | 0.1.9 |
| OCR | Tesseract CLI | 5.x (system) |
| Excel Export | ClosedXML | 0.104.2 |
| CSS Framework | Bootstrap | 5.x |
| Deployment | Docker + Railway | Latest |

## 📊 Output Format

Bảng Excel có **17 cột** theo thứ tự:

1. **STT** - Số thứ tự
2. **origin** - Xuất xứ (EGYPT, GREECE, etc.)
3. **crop year - vụ mùa** - Năm vụ (2024/25)
4. **số lượng** - Quantity (MT)
5. **loại bông** - Cotton type
6. **chỉ tiêu đặc biệt** - Special specifications
7. **Màu sắc** - Color (Rd value hoặc grade)
8. **tạp lá** - Leaf content
9. **chiều dài** - Length (inch)
10. **Micronaire** - Micronaire value
11. **cường lực Min** - Minimum strength (GPT)
12. **Basis** - Basis points
13. **Shipment Date** - Ngày giao hàng
14. **giá (c/kg)** - Price in cents/kg
15. **giá có Commission** - Price after commission
16. **giá net (Toyoshima)** - Net price
17. **Ghi chú** - Notes

## � Công thức Tính giá

Basis trong Offer là **cents** (không phải points/100).

```
Outright (c/lb) = ICE Settlement + Basis (cents)
Giá (c/kg) = Outright (c/lb) × 2.20462
Giá có Commission = Giá (c/kg) - Commission (c/kg)
```

**Ví dụ** (ME066M6):
- ICE JUL'26: 84.19
- Basis: 11.00 cents
- Commission: 2.00 c/kg

```
Outright = 84.19 + 11.00 = 95.19 c/lb
Giá (c/kg) = 95.19 × 2.20462 = 209.86 c/kg
Giá net = 209.86 - 2.00 = 207.86 c/kg
```

## 🚀 Deployment Options

### Production: Railway.app (Đang dùng)
- ✅ Auto-deploy từ GitHub `main` branch
- ✅ PostgreSQL addon
- ✅ Docker build tự động
- ✅ SSL certificate
- **URL**: `https://cottonagent-production-92e0.up.railway.app`

## 📚 Documentation Files

| File | Purpose |
|------|---------|
| `README.md` | Main documentation, installation, usage |
| `QUICKSTART.md` | 5-minute quick start guide |
| `DEPLOYMENT.md` | Detailed deployment instructions |
| `TESTING.md` | Test cases and testing guide |
| `CHANGELOG.md` | Version history and changes |
| `PROJECT_SUMMARY.md` | This file - project overview |

## 🔐 Security Features

- ✅ Password hashing (SHA256)
- ✅ SQL injection protection (EF Core)
- ✅ XSS protection (Blazor auto-escaping)
- ✅ File type validation
- ✅ File size limits (10MB)
- ✅ HTTPS enforcement
- ✅ Session management

## ⚡ Performance

- ✅ Process 1 offer + 10 HVI < 30 seconds
- ✅ Support up to 20 HVI files simultaneously
- ✅ Database connection pooling
- ✅ Efficient PDF parsing
- ✅ Optimized Excel generation

## 🧪 Testing

### Manual Testing
- ✅ 10 test cases documented in TESTING.md
- ✅ Database integrity tests
- ✅ Security tests
- ✅ Performance tests

### Automated Testing (Future)
- ⏳ Unit tests
- ⏳ Integration tests
- ⏳ End-to-end tests

## 📝 Default Credentials

### Application
- Username: `admin`
- Password: `admin123`
- ⚠️ **CHANGE IN PRODUCTION!**

### PgAdmin (Local)
- URL: http://localhost:5050
- Email: `admin@cbas.local`
- Password: `admin123`

### PostgreSQL (Local)
- Host: `localhost`
- Port: `5432`
- Database: `cbas_db`
- Username: `postgres`
- Password: `postgres`

## 🎯 Next Steps (Giai đoạn 2)

### Planned Features
1. **Bid Matching**
   - Upload Bid từ nhà máy
   - Auto-matching Offer vs Bid
   - Comparison reports

2. **Dashboard**
   - Statistics và charts
   - Offer history
   - Search và filtering

3. **Advanced Features**
   - Email notifications
   - Multi-user với roles
   - Audit logging
   - API for mobile app

4. **UI Improvements**
   - Dark mode
   - Multi-language (EN/VI)
   - Drag & drop upload

## 📞 Support & Contact

- **Email**: support@cbas.local
- **Documentation**: See README.md
- **Issues**: Report via email

## 🎉 Project Status

**v1.1 DEPLOYED ✅**

- ✅ Production deployment on Railway
- ✅ OCR integration (Tesseract CLI)
- ✅ 3-step workflow
- ✅ Real Toyoshima format parser
- ✅ User acceptance testing (UAT)

## 📦 Deliverables

1. ✅ Complete source code
2. ✅ Database schema và migrations
3. ✅ Comprehensive documentation
4. ✅ Docker setup for local development
5. ✅ Deployment configurations
6. ✅ Testing guidelines
7. ✅ Quick start guide

## 🏆 Success Criteria - ALL MET

- ✅ Upload Offer PDF và parse tự động
- ✅ Upload nhiều HVI PDF và parse tự động
- ✅ Liên kết HVI với lô tự động
- ✅ Tính giá chính xác theo công thức
- ✅ Hiển thị bảng output đúng format (17 cột)
- ✅ Export Excel format chuẩn
- ✅ Xử lý < 30 giây cho 20 files
- ✅ Authentication đơn giản
- ✅ Ready for Heroku deployment
- ✅ Full documentation

---

**Dự án CBAS v1.1 đã deployed và sẵn sàng sử dụng!** 🎊
