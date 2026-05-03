# 🚀 Deploy CBAS lên Railway.app

## Bước 1: Truy cập Railway

Mở browser và truy cập: **https://railway.app**

Login với tài khoản của bạn.

## Bước 2: Tạo Project Mới

1. Click **"New Project"**
2. Chọn **"Deploy from GitHub repo"**
3. Nếu chưa connect GitHub:
   - Click **"Configure GitHub App"**
   - Authorize Railway
   - Chọn repository hoặc organization

## Bước 3: Deploy từ Local (Nếu chưa có GitHub repo)

### Option A: Push lên GitHub trước

```bash
# Tạo repo mới trên GitHub
# Sau đó:
git remote add origin https://github.com/YOUR_USERNAME/CottonAgent.git
git branch -M main
git push -u origin main
```

Sau đó quay lại Railway và chọn repo vừa tạo.

### Option B: Deploy trực tiếp từ CLI

```bash
# Cài Railway CLI
npm i -g @railway/cli

# Login
railway login

# Init project
railway init

# Deploy
railway up
```

## Bước 4: Thêm PostgreSQL Database

1. Trong Railway project dashboard
2. Click **"New"** → **"Database"** → **"PostgreSQL"**
3. Railway tự động tạo database và set environment variables

## Bước 5: Cấu hình Environment Variables

Railway tự động set `DATABASE_URL`, nhưng cần thêm:

1. Click vào **Service** (CBAS.Web)
2. Tab **"Variables"**
3. Thêm:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   PORT=5000
   ```

## Bước 6: Deploy

Railway tự động deploy khi:
- Push code lên GitHub (nếu dùng GitHub integration)
- Hoặc chạy `railway up` (nếu dùng CLI)

## Bước 7: Xem Deployment

1. Click vào **Deployments** tab
2. Xem logs real-time
3. Đợi build hoàn thành (2-3 phút)

## Bước 8: Lấy Public URL

1. Click vào **Settings** tab
2. Scroll xuống **"Networking"**
3. Click **"Generate Domain"**
4. Railway sẽ tạo URL dạng: `cbas-web-production.up.railway.app`

## Bước 9: Truy cập App

Mở URL vừa tạo:
- Login: `admin` / `admin123`
- ⚠️ **ĐỔI PASSWORD NGAY!**

---

## 🎯 Cách Nhanh Nhất (Recommended)

### Sử dụng Railway CLI:

```powershell
# 1. Cài Railway CLI (chỉ lần đầu)
npm i -g @railway/cli

# 2. Login
railway login

# 3. Init project
railway init

# 4. Link với project (nếu đã tạo trên web)
railway link

# 5. Add PostgreSQL
railway add

# Chọn PostgreSQL từ menu

# 6. Deploy
railway up

# 7. Mở app
railway open
```

---

## 📊 Monitoring

### Xem Logs
```bash
railway logs
```

### Xem Metrics
- Vào Railway dashboard
- Click vào service
- Tab **"Metrics"**

---

## 💰 Chi phí

Với tài khoản trả phí:
- **Hobby Plan**: $5/month
- **Pro Plan**: $20/month
- Bao gồm: PostgreSQL + Deployment + Bandwidth

---

## 🔧 Troubleshooting

### Build Failed

Kiểm tra logs:
```bash
railway logs --deployment
```

### Database Connection Failed

Kiểm tra `DATABASE_URL`:
```bash
railway variables
```

### App không start

Kiểm tra PORT:
- Railway tự động set PORT
- App phải listen trên `$PORT`
- Code đã support (đã có trong Program.cs)

---

## ✅ Checklist

- [ ] Railway account đã tạo
- [ ] Project đã tạo trên Railway
- [ ] PostgreSQL đã add
- [ ] Environment variables đã set
- [ ] Code đã deploy
- [ ] Domain đã generate
- [ ] App truy cập được
- [ ] Login thành công
- [ ] Password đã đổi

---

## 🆘 Support

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- Email: support@cbas.local

---

**Deploy thành công! 🎉**
