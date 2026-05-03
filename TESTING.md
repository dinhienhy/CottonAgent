# Hướng dẫn Test Hệ thống CBAS

## Setup môi trường Test

### 1. Khởi động PostgreSQL bằng Docker

```bash
docker-compose up -d
```

Kiểm tra:
- PostgreSQL: `localhost:5432`
- PgAdmin: `http://localhost:5050`
  - Email: admin@cbas.local
  - Password: admin123

### 2. Chạy Migration

```bash
dotnet ef database update
```

### 3. Khởi động ứng dụng

```bash
dotnet run
```

Truy cập: `https://localhost:5001`

## Test Cases

### TC01: Login

**Mục đích**: Kiểm tra chức năng đăng nhập

**Bước thực hiện**:
1. Truy cập `/login`
2. Nhập username: `admin`
3. Nhập password: `admin123`
4. Click "Login"

**Kết quả mong đợi**:
- Redirect đến `/offer-processor`
- Hiển thị trang upload

### TC02: Upload Offer PDF

**Mục đích**: Kiểm tra upload và parse Offer PDF

**Dữ liệu test**: Cần file PDF mẫu từ Toyoshima

**Bước thực hiện**:
1. Login thành công
2. Truy cập `/offer-processor`
3. Nhập Supplier Name: "Toyoshima"
4. Nhập ICE Value: 84.19
5. Nhập Commission: 2.00
6. Upload file Offer PDF
7. Click "Process Offer"

**Kết quả mong đợi**:
- File upload thành công
- Parsing thành công
- Hiển thị danh sách lô (nếu có trong PDF)

### TC03: Upload HVI Reports

**Mục đích**: Kiểm tra upload nhiều HVI PDF

**Dữ liệu test**: Nhiều file HVI PDF (ME066M6.pdf, ME067M6.pdf...)

**Bước thực hiện**:
1. Sau khi upload Offer PDF
2. Click "Choose Files" ở HVI Reports
3. Chọn nhiều file PDF (Ctrl + Click)
4. Xác nhận danh sách file hiển thị
5. Click "Process Offer"

**Kết quả mong đợi**:
- Tất cả file được upload
- Parse thành công từng file
- Liên kết HVI với lô dựa trên Lot Code

### TC04: Tính giá tự động

**Mục đích**: Kiểm tra công thức tính giá

**Dữ liệu test**:
- ICE: 84.19
- Basis: 150 points
- Commission: 2.00 c/kg

**Công thức**:
```
Giá (c/lb) = 84.19 + (150 / 100) = 85.69
Giá (c/kg) = 85.69 × 2.20462 = 188.93
Giá có Commission = 188.93 - 2.00 = 186.93
```

**Kết quả mong đợi**:
- Giá (c/kg): 188.93
- Giá có Commission: 186.93
- Giá net: 186.93

### TC05: Hiển thị bảng Output

**Mục đích**: Kiểm tra format bảng output

**Bước thực hiện**:
1. Sau khi process thành công
2. Kiểm tra bảng hiển thị

**Kết quả mong đợi**:
- Có 17 cột theo đúng thứ tự
- Dữ liệu được nhóm theo Shipment Date
- Tiêu đề nhóm: "DD/MM/YYYY SO"
- STT tăng dần

### TC06: Export Excel

**Mục đích**: Kiểm tra xuất file Excel

**Bước thực hiện**:
1. Sau khi hiển thị bảng output
2. Click "Export Excel"
3. Mở file Excel vừa download

**Kết quả mong đợi**:
- File .xlsx download thành công
- Mở được bằng Excel
- Format đúng với 17 cột
- Có header row
- Có nhóm theo Shipment Date
- Dữ liệu chính xác

### TC07: Xử lý nhiều lô

**Mục đích**: Test performance với nhiều lô

**Dữ liệu test**:
- 1 Offer PDF với 20 lô
- 20 HVI PDF tương ứng

**Bước thực hiện**:
1. Upload Offer + 20 HVI files
2. Click "Process Offer"
3. Đo thời gian xử lý

**Kết quả mong đợi**:
- Thời gian < 30 giây
- Tất cả lô được parse
- Tất cả HVI được liên kết đúng

### TC08: Xử lý lỗi - File không hợp lệ

**Mục đích**: Kiểm tra xử lý lỗi

**Bước thực hiện**:
1. Upload file không phải PDF
2. Hoặc upload PDF bị corrupt
3. Click "Process Offer"

**Kết quả mong đợi**:
- Hiển thị error message rõ ràng
- Không crash app
- User có thể thử lại

### TC09: Xử lý lỗi - HVI không match

**Mục đích**: Kiểm tra khi HVI không match với lô

**Dữ liệu test**:
- Offer có lô ME066M6
- HVI có lot code ME999X9 (không tồn tại)

**Kết quả mong đợi**:
- Lô vẫn hiển thị
- Các trường HVI để trống
- Không crash

### TC10: Reset Form

**Mục đích**: Kiểm tra reset về form mới

**Bước thực hiện**:
1. Sau khi process thành công
2. Click "New Offer"

**Kết quả mong đợi**:
- Quay về form upload
- Form trống
- Giá trị mặc định được restore

## Test Database

### Kiểm tra dữ liệu trong database

```sql
-- Kiểm tra Offers
SELECT * FROM "Offers" ORDER BY "CreatedAt" DESC;

-- Kiểm tra OfferLots
SELECT * FROM "OfferLots" WHERE "OfferId" = 1;

-- Kiểm tra HVIReports
SELECT * FROM "HVIReports";

-- Kiểm tra ProcessedOutputs
SELECT * FROM "ProcessedOutputs" WHERE "OfferId" = 1 ORDER BY "STT";

-- Kiểm tra User
SELECT * FROM "Users";
```

### Kiểm tra liên kết

```sql
-- Kiểm tra lô có HVI
SELECT 
    ol."LotCode",
    ol."Origin",
    hvi."Micronaire",
    hvi."Length",
    hvi."StrengthGPT"
FROM "OfferLots" ol
LEFT JOIN "HVIReports" hvi ON ol."LotCode" = hvi."LotCode"
WHERE ol."OfferId" = 1;
```

## Performance Testing

### Test với Apache Bench

```bash
# Test 100 requests, 10 concurrent
ab -n 100 -c 10 https://localhost:5001/
```

### Test với k6

Tạo file `load-test.js`:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

export default function () {
  let res = http.get('https://localhost:5001/offer-processor');
  check(res, { 'status was 200': (r) => r.status == 200 });
  sleep(1);
}
```

Chạy:
```bash
k6 run load-test.js
```

## Security Testing

### Test SQL Injection

Thử nhập vào username:
```
admin' OR '1'='1
```

**Kết quả mong đợi**: Login fail, không bị inject

### Test XSS

Thử nhập vào Supplier Name:
```html
<script>alert('XSS')</script>
```

**Kết quả mong đợi**: Text được escape, không execute script

### Test File Upload

Thử upload file:
- File > 10MB
- File .exe
- File .zip chứa virus

**Kết quả mong đợi**: Reject file không hợp lệ

## Regression Testing

Sau mỗi lần update code, chạy lại:

1. TC01 - TC10
2. Database integrity check
3. Performance test
4. Security test

## Bug Report Template

```markdown
**Bug ID**: BUG-001
**Severity**: High/Medium/Low
**Module**: Offer Processing
**Description**: Mô tả bug
**Steps to Reproduce**:
1. Step 1
2. Step 2
**Expected Result**: Kết quả mong đợi
**Actual Result**: Kết quả thực tế
**Screenshots**: Attach ảnh
**Environment**: Windows 11, Chrome 120
**Date Found**: 03/05/2026
```

## Test Data

### Sample Offer Data

```
Lot Code: ME066M6
Origin: EGYPT
Crop Year: 2024/25
Quantity: 100 MT
Type: Giza 86
Basis: +150 pts
Shipment: 15/06/2026
```

### Sample HVI Data

```
Lot Code: ME066M6
Micronaire: 4.2
Length: 1.125
Strength: 30.5
Uniformity: 84.5
Color Rd: 75.2
Leaf: 2
```

## Automated Testing (Future)

### Unit Tests

```csharp
[Fact]
public void CalculatePrice_ShouldReturnCorrectValue()
{
    // Arrange
    decimal ice = 84.19m;
    decimal basis = 150m;
    
    // Act
    var result = PriceCalculator.CalculatePricePerKg(ice, basis);
    
    // Assert
    Assert.Equal(188.93m, result, 2);
}
```

### Integration Tests

```csharp
[Fact]
public async Task ProcessOffer_ShouldCreateOutput()
{
    // Arrange
    var offer = CreateTestOffer();
    
    // Act
    var outputId = await _service.ProcessOfferAsync(offer);
    
    // Assert
    Assert.True(outputId > 0);
}
```

## Checklist trước khi Release

- [ ] Tất cả test cases pass
- [ ] Database migration chạy thành công
- [ ] Performance test đạt yêu cầu (< 30s cho 20 files)
- [ ] Security test pass
- [ ] Backup database
- [ ] Update README
- [ ] Update CHANGELOG
- [ ] Tag version trong Git
- [ ] Deploy lên staging
- [ ] UAT với user
- [ ] Deploy lên production

## Contact

Báo bug: bugs@cbas.local
Support: support@cbas.local
