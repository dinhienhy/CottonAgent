# Changelog

All notable changes to the Cotton Broker Automation System (CBAS) will be documented in this file.

## [2.1.0] - 2026-05-13

### Fixed — Lot Data Persistence & Prompt Caching

#### Prompt Caching (Claude API)
- **`anthropic-beta: prompt-caching-2024-07-31`** header — giảm chi phí & tăng tốc parse
- System prompt + few-shot example dùng `cache_control: ephemeral` blocks
- `max_tokens` tăng lên 32000 để tránh JSON bị truncate
- `ClaudeParseLog` DTO — thêm `CacheCreationTokens`, `CacheReadTokens`
- UI hiện badge **Cache HIT** / **Cache CREATED** trong AI Log panel

#### HttpClient Timeout
- Tăng timeout Claude API từ 100s → **300s** (`Program.cs`) — tránh cancel khi parse offer lớn

#### Fix: LotCode & Origin mapping
- `PdfParserService.ConvertAIResult` — sửa `LotCode = lot.KieuBong` (trước đó null) và `Origin = lot.LoaiBong` (trước đó luôn = shipper name)

#### Fix: Lot Data Persistence (SyncLotsAsync)
- **Per-lot try-catch** — 1 lot lỗi không abort toàn bộ vòng lặp
- **ChangeTracker cleanup** — detach Added / reload Modified entities khi lỗi
- **Diagnostic counters** — total, created, updated, skipped, errors, unique codes, total DB count
- **Sync DB log hiện trên UI** (Step 3) — hiện rõ kết quả sync

#### Fix: Unique LotCode per OfferLot
- Auto-generate LotCode cho lots mà AI không trích xuất được `kieu_bong` (format: `{Origin}-001`)
- **Deduplicate suffix** — nếu cùng LotCode xuất hiện nhiều lần trong offer, thêm suffix `-02`, `-03`...
- Mỗi dòng offer = 1 lot riêng biệt trong DB (trước đó trùng LotCode → merge mất data)

#### UI Improvements
- **LotList**: badge `xx lots` hiện tổng số lot cạnh tiêu đề
- **LotList**: nút "Xóa (n)" — xóa nhiều lots đã chọn cùng lúc
- **Settings**: sửa model dropdown dùng đúng Anthropic model IDs

---

## [2.0.0] - 2026-05-07

### Added - AI Universal Offer Parser (Claude API + Few-Shot Learning)

#### AI Parser Core
- **Claude API integration** — gọi Anthropic Claude API để parse Offer PDF
- **Few-shot learning** — lần đầu upload Excel mẫu → AI học format, lần sau tự động parse
- **Hybrid strategy** — AI là primary parser, regex parsers (Toyoshima/Olam/Brighann) là fallback
- **System prompt chuyên biệt** — decode đúng spec bông Mỹ (GC/SM/EMOT, G5≠Color Grade)

#### New Model: ShipperSample
- Lưu cặp PDF mẫu + Excel kết quả mong muốn per shipper (1 cặp/shipper)
- Tự extract PDF text + Excel JSON để dùng trong prompt (few-shot examples)
- Migration: `20260506100000_AddShipperSamples`

#### New Services
- `IClaudeParserService` / `ClaudeParserService` — gọi Claude API, parse JSON response
- `PdfParserService.ParseOfferPdfWithAIAsync()` — AI primary → regex fallback
- `ClaudeOfferResponse` DTO — JSON schema cho AI response

#### UI Updates (OfferProcessor.razor)
- Checkbox "Lần đầu với shipper này" → upload Excel mẫu
- Tự động lưu ShipperSample khi submit
- Extract Excel content (ClosedXML) thành JSON cho few-shot prompt

#### Config
- `ANTHROPIC_API_KEY` environment variable
- `Anthropic:Model` in appsettings.json (default: claude-sonnet-4-20250514)

---

## [1.3.0] - 2026-05-04

### Upgraded - Quản lý Lot hàng & Tạo Output Chào Hàng

#### Trang Quản lý Lot (`/lots`) — rewrite hoàn toàn
- **23 cột đầy đủ**: STT, Lot Code, Shipper, Origin, Crop Year, Type, Spec, Shipment, QTY Orig, QTY Avail, Color, Leaf, Length, Mic, Str Min, Basis, c/kg, +Comm, Net, Status, HVI, Action
- **ICE + Commission inputs** ở góc trên phải — thay đổi → giá tự động refresh realtime
- **Multi-select** checkbox từng row + Select All
- **Tạo Output Chào Hàng**: chọn nhiều lots → export Excel (nhóm theo Shipment Date, format chuẩn)
- **HVI detail modal**: click "HVI ✓" → xem/chỉnh Mic, Length, Strength, Uniformity, Color Rd, Leaf
- **Filter mới**: Shipment month (type=month input)
- Basis hiển thị dạng **points** (VD: 1100, 1080)

#### Lot model — thêm fields
- `BasisCents`, `ShipmentDate`, `ShipmentDateText`, `SpecialSpec`
- Migration `20260504070000_AddLotDisplayFields`
- `SyncLotsAsync` copy các fields từ OfferLot → Lot khi tạo/update

#### Fix: Basis = 0 cho lots cũ
- Startup backfill: khi app khởi động, tìm lots có BasisCents=0, populate từ OfferLot data
- Công thức giá: `(ICE + BasisCents) × 2.20462 = c/kg` ✓

---

## [1.2.0] - 2026-05-04

### Added - Phase 2A: Shipper/Lot Management

#### New Models
- `Shipper` entity — quản lý nhà cung cấp (Name unique, ContactInfo, Email, Phone, IsActive)
- `Lot` master entity — quản lý lô hàng (LotCode unique, QuantityOriginal, QuantityAvailable, Status: Available/Reserved/Sold)

#### Updated Models
- `Offer` → thêm `ShipperId` FK liên kết Shipper
- `OfferLot` → thêm `MasterLotId` FK liên kết Lot master
- `HVIReport` → thêm `MasterLotId` FK liên kết Lot master

#### Auto-sync Logic (OfferProcessingService)
- Upload Offer → tự động tìm/tạo Shipper từ SupplierName, gán vào Offer
- Recalculate → `SyncLotsAsync`: tìm/tạo Lot cho mỗi LotCode, update QuantityAvailable, link OfferLot→Lot, link HVIReport→Lot

#### New Pages
- `/shippers` — CRUD quản lý shipper (thêm/sửa/bật/tắt)
- `/offers` — Lịch sử offers (filter theo shipper, ngày)
- `/lots` — Inventory lots (filter shipper/origin/status/lotcode, summary cards, thay đổi status)

#### UI Improvements
- Offer Processor: thay InputText SupplierName bằng dropdown chọn Shipper từ DB
- NavMenu: thêm links Lots, Offers, Shippers

---

## [1.1.0] - 2026-05-04

### Added - Phase 2: OCR & Parser Rewrite

#### Tesseract OCR Integration
- Tesseract OCR cho HVI scan PDFs (CLI mode — gọi `tesseract` trực tiếp, tránh lỗi native lib)
- Extract ảnh từ PDF bằng PdfPig `TryGetPng()`, lưu temp file, chạy `tesseract img stdout -l eng --psm 3`
- Parse kết quả OCR bằng regex để trích xuất HVI fields (Mic, Len, Str, Unif, Rd, Leaf, etc.)
- Graceful degradation: OCR unavailable → manual input only

#### 3-Step Workflow (OfferProcessor.razor)
- **Step 1**: Upload Offer PDF + HVI PDFs
- **Step 2**: HVI Review — OCR tự động chạy, hiển thị confidence score, raw text, cho phép edit/nhập tay
- **Step 3**: Results — bảng Output + Export Excel

#### Offer PDF Parser Rewrite
- Parser viết lại hoàn toàn cho format Toyoshima thật
- Sử dụng **word-position grouping** (Y-coordinate rows) thay vì regex on blob text
- Hỗ trợ multiple sections by origin (USA, Brazil, Greece, Australia, Argentina, Mexico)
- Hỗ trợ generic lines và M/E Recap lines
- ICE settlements extraction: JUL'26=84.19, DEC'26=84.56, MAR'27=85.26

#### Model Updates
- `OfferLot.BasisPoints` → `BasisCents` (basis trong offer là cents, không phải points)
- `OfferLot.LotCode` → nullable (generic lines không có lot code)
- Thêm: `OutrightPrice`, `SettlementMonth`, `ShipmentDateText`, spec fields
- `Offer.ICESettlementsJson` — lưu ICE settlement months
- `HVIInputDto` — thêm `ConfidenceScore`, `RawOcrText`, `OcrStatus`

#### New Files
- `Services/IOcrService.cs` — OCR service interface
- `Services/TesseractOcrService.cs` — Tesseract CLI implementation
- `DTOs/OcrResult.cs` — OCR result DTO
- `DTOs/HVIInputDto.cs` — HVI input/review DTO

#### Docker & Deployment
- Dockerfile: cài `tesseract-ocr` + `tesseract-ocr-eng` trong runtime stage
- Deploy tự động lên Railway qua GitHub push
- URL: `https://cottonagent-production-92e0.up.railway.app`

### Fixed
- Blazor Server `_blazorFilesById` bug: thêm `@key` trên InputFile, per-file error handling
- SignalR `KeepAliveInterval` = 10s để tránh disconnect khi đọc file
- Tesseract NuGet native lib mismatch (`libleptonica-1.82.0.so`) → chuyển sang CLI mode

### Removed
- `Tesseract` NuGet package 5.2.0 (thay bằng CLI)

---

## [1.0.0] - 2026-05-03

### Added - Giai đoạn 1

#### Core Features
- ✅ Upload Offer PDF từ shipper (Toyoshima và suppliers khác)
- ✅ Upload nhiều HVI Report PDF (tối đa 20 files)
- ✅ Parse tự động Offer PDF để trích xuất thông tin lô
- ✅ Parse tự động HVI PDF để lấy chỉ số kỹ thuật
- ✅ Tự động liên kết HVI với lô dựa trên Lot Code
- ✅ Tính giá tự động theo công thức: (ICE + Basis/100) × 2.20462
- ✅ Áp dụng Commission (mặc định 2 c/kg, có thể chỉnh)
- ✅ Hiển thị bảng Output với 17 cột theo format chuẩn
- ✅ Nhóm dữ liệu theo Shipment Date
- ✅ Export Excel (.xlsx) với format chuẩn

#### Technical Stack
- ✅ ASP.NET Core 8.0 Blazor Server
- ✅ PostgreSQL với Entity Framework Core
- ✅ PdfPig cho PDF parsing
- ✅ ClosedXML cho Excel export
- ✅ Bootstrap 5 cho UI

#### Database Schema
- ✅ Bảng Offers (lưu thông tin offer chính)
- ✅ Bảng OfferLots (lưu từng lô trong offer)
- ✅ Bảng HVIReports (lưu HVI data)
- ✅ Bảng ProcessedOutputs (lưu output đã xử lý)
- ✅ Bảng Users (authentication)

#### Authentication
- ✅ Simple login với username/password
- ✅ SHA256 password hashing
- ✅ Default user: admin/admin123
- ✅ Session management

#### UI/UX
- ✅ Responsive design với Bootstrap 5
- ✅ Upload progress indicator
- ✅ Error handling và hiển thị message
- ✅ Loading states
- ✅ Clean và modern interface

#### Documentation
- ✅ README.md với hướng dẫn cài đặt và sử dụng
- ✅ DEPLOYMENT.md với hướng dẫn deploy lên Heroku/Railway/Azure
- ✅ TESTING.md với test cases và hướng dẫn test
- ✅ CHANGELOG.md
- ✅ Code comments và documentation

#### DevOps
- ✅ Docker Compose cho local development
- ✅ Entity Framework Migrations
- ✅ Procfile cho Heroku deployment
- ✅ .gitignore configured
- ✅ Auto-migration on startup

### Output Format

Bảng output có 17 cột theo thứ tự:
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

### Known Limitations (Giai đoạn 1)

- ⚠️ PDF parsing dựa trên regex patterns - có thể cần điều chỉnh cho format khác
- ⚠️ Chưa có matching hai chiều với Bid từ nhà máy (sẽ làm giai đoạn 2)
- ⚠️ Authentication đơn giản, chưa có role-based access control
- ⚠️ Chưa có dashboard thống kê
- ⚠️ Chưa có API cho mobile app
- ⚠️ Chưa có email notification
- ⚠️ Chưa có audit log

### Performance

- ✅ Xử lý 1 offer + 10 HVI < 30 giây
- ✅ Hỗ trợ tối đa 20 HVI files cùng lúc
- ✅ File size tối đa: 10MB per file

### Security

- ✅ Password hashing với SHA256
- ✅ SQL injection protection (Entity Framework)
- ✅ XSS protection (Blazor auto-escaping)
- ✅ File type validation
- ✅ File size limits

## [Planned for 2.0.0] - Future

### Features to be added in Phase 2

#### Bid Matching
- [ ] Upload Bid từ nhà máy Việt Nam
- [ ] Matching tự động Offer vs Bid
- [ ] Hiển thị kết quả matching
- [ ] Suggest best matches
- [ ] Export comparison report

#### Advanced Features
- [ ] Dashboard với charts và statistics
- [ ] Lịch sử offers và search
- [ ] Email notification khi có offer mới
- [ ] Multi-user support với roles
- [ ] Audit log cho tất cả actions
- [ ] Backup/restore database từ UI
- [ ] Template management cho PDF parsing

#### API & Integration
- [ ] REST API cho mobile app
- [ ] Webhook integration
- [ ] Export to other formats (CSV, JSON)
- [ ] Import from Excel
- [ ] Integration với accounting system

#### UI Improvements
- [ ] Dark mode
- [ ] Multi-language support (EN/VI)
- [ ] Advanced filtering và sorting
- [ ] Bulk operations
- [ ] Drag & drop file upload
- [ ] Preview PDF before processing

#### Performance
- [ ] Background job processing
- [ ] Caching layer
- [ ] CDN for static assets
- [ ] Database optimization
- [ ] Parallel PDF processing

#### Security
- [ ] Two-factor authentication
- [ ] OAuth2 integration
- [ ] Role-based access control
- [ ] API key management
- [ ] Rate limiting
- [ ] Advanced password policy

## Version History

### Version Numbering

Format: MAJOR.MINOR.PATCH

- MAJOR: Breaking changes
- MINOR: New features (backward compatible)
- PATCH: Bug fixes

### Release Notes

#### v1.0.0 (2026-05-03)
- Initial release
- Core functionality complete
- Ready for production use
- Tested with sample data

## Migration Guide

### From v0.x to v1.0.0

N/A - This is the first release

### Database Migrations

```bash
# Apply all migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update <MigrationName>

# Generate new migration
dotnet ef migrations add <MigrationName>
```

## Breaking Changes

### v1.0.0
- None (initial release)

## Deprecations

### v1.0.0
- None (initial release)

## Bug Fixes

### v1.0.0
- None (initial release)

## Contributors

- Agent Bông Việt Nam - Product Owner
- Development Team - Implementation

## Support

- Email: support@cbas.local
- Documentation: See README.md
- Issues: Report via email or GitHub Issues

## License

Proprietary - All rights reserved
Copyright © 2026 Agent Bông Việt Nam
