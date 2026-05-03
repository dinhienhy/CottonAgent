# 🚀 Deploy NGAY lên Heroku

## Cách 1: Dùng Script Tự động (Khuyến nghị)

### Windows
```powershell
.\deploy-heroku.ps1
```

### Linux/Mac
```bash
chmod +x deploy-heroku.sh
./deploy-heroku.sh
```

Script sẽ tự động:
1. ✅ Kiểm tra Heroku CLI và Git
2. ✅ Login vào Heroku
3. ✅ Tạo app mới
4. ✅ Thêm PostgreSQL database
5. ✅ Set stack và environment
6. ✅ Deploy code
7. ✅ Mở app trong browser

---

## Cách 2: Manual Commands (Nếu script không chạy)

### Bước 1: Login
```bash
heroku login
```

### Bước 2: Tạo app
```bash
# Tên app tự động
heroku create

# Hoặc tên tùy chỉnh
heroku create cotton-broker-system
```

### Bước 3: Thêm PostgreSQL
```bash
# Lấy tên app từ bước 2
heroku addons:create heroku-postgresql:mini -a YOUR_APP_NAME
```

### Bước 4: Set stack
```bash
heroku stack:set container -a YOUR_APP_NAME
```

### Bước 5: Deploy
```bash
git push heroku master
```

### Bước 6: Mở app
```bash
heroku open -a YOUR_APP_NAME
```

---

## ⚠️ Lưu ý QUAN TRỌNG

### 1. Yêu cầu
- ✅ Tài khoản Heroku (free)
- ✅ Credit card để verify (bắt buộc)
- ✅ Heroku CLI đã cài: https://devcenter.heroku.com/articles/heroku-cli

### 2. Chi phí
- **Eco Dyno**: $5/month (550 hours)
- **Mini PostgreSQL**: $5/month
- **Tổng**: ~$10/month

### 3. Sau khi deploy
- 🔐 Login: `admin` / `admin123`
- ⚠️ **ĐỔI PASSWORD NGAY!**
- 📧 Email: admin@cbas.local

---

## 🔍 Kiểm tra Deployment

### Xem logs
```bash
heroku logs --tail -a YOUR_APP_NAME
```

### Kiểm tra status
```bash
heroku ps -a YOUR_APP_NAME
```

### Kiểm tra database
```bash
heroku pg:info -a YOUR_APP_NAME
```

---

## ❌ Troubleshooting

### Lỗi: "Heroku CLI not found"
```bash
# Windows (PowerShell as Admin)
choco install heroku-cli

# Mac
brew tap heroku/brew && brew install heroku

# Linux
curl https://cli-assets.heroku.com/install.sh | sh
```

### Lỗi: "Git not found"
```bash
# Windows
choco install git

# Mac
brew install git

# Linux
sudo apt-get install git
```

### Lỗi: "Payment method required"
Heroku yêu cầu credit card để verify tài khoản:
1. Vào https://dashboard.heroku.com/account/billing
2. Thêm credit card
3. Thử deploy lại

### Lỗi: "Build failed"
```bash
# Xem logs chi tiết
heroku logs --tail -a YOUR_APP_NAME

# Thử build lại
git commit --allow-empty -m "Rebuild"
git push heroku master
```

---

## 🎯 Alternative: Railway.app (Dễ hơn!)

Nếu gặp khó khăn với Heroku:

### Bước 1: Truy cập Railway
https://railway.app

### Bước 2: Login với GitHub
Click "Login with GitHub"

### Bước 3: Deploy
1. Click "New Project"
2. Chọn "Deploy from GitHub repo"
3. Authorize Railway
4. Chọn repo CottonAgent
5. Railway tự động detect và deploy!

### Bước 4: Add Database
1. Click "New" → "Database" → "PostgreSQL"
2. Railway tự động connect

**Ưu điểm Railway**:
- ✅ Dễ hơn nhiều
- ✅ Free tier tốt ($5 credit/month)
- ✅ Không cần credit card cho free tier
- ✅ GitHub integration tự động
- ✅ Tự động deploy khi push code

---

## 📞 Cần Hỗ trợ?

### Heroku Documentation
- https://devcenter.heroku.com/articles/getting-started-with-dotnet

### Railway Documentation
- https://docs.railway.app/

### Email Support
- support@cbas.local

---

## ✅ Checklist Deploy

- [ ] Heroku CLI đã cài
- [ ] Git đã cài
- [ ] Đã login Heroku
- [ ] Đã thêm credit card (Heroku)
- [ ] Chạy script hoặc manual commands
- [ ] App deploy thành công
- [ ] Database đã tạo
- [ ] Truy cập app URL được
- [ ] Login thành công
- [ ] ĐÃ ĐỔI PASSWORD ADMIN

---

**Chúc bạn deploy thành công! 🎉**

Sau khi deploy xong, app sẽ có URL dạng:
`https://your-app-name.herokuapp.com`

Login và bắt đầu sử dụng ngay!
