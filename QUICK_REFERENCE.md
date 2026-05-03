# 🚀 CBAS Quick Reference Guide

## 📌 **Essential Links**

| Resource | URL |
|----------|-----|
| **Production App** | https://cottonagent-production-92e0.up.railway.app/ |
| **GitHub Repo** | https://github.com/dinhienhy/CottonAgent |
| **Railway Dashboard** | https://railway.app/dashboard |
| **Neon.tech Console** | https://console.neon.tech |

## 🔑 **Login Credentials**

```
Username: admin
Password: admin123
```

⚠️ **IMPORTANT:** Change password after first login!

---

## 💻 **Quick Commands**

### **Run Locally**
```powershell
# Start app with Neon.tech database
.\run-local.ps1

# Access at: http://localhost:5000
```

### **Deploy to Railway**
```powershell
# Deploy via GitHub
git add -A
git commit -m "Your message"
git push origin main

# Railway auto-deploys in 1-2 minutes
```

### **Manual Railway Deploy**
```powershell
.\deploy-to-railway.ps1
```

### **View Railway Logs**
```powershell
# In Railway Dashboard:
# Service → Deployments → Click deployment → View logs
```

---

## 🐛 **Common Issues & Fixes**

### **Issue: App won't start locally**
```powershell
# Check if port 5000 is in use
netstat -ano | findstr :5000

# Kill process if needed
taskkill /F /PID <PID>
```

### **Issue: Database connection error**
```powershell
# Check DATABASE_URL is set
echo $env:DATABASE_URL

# Set it manually
$env:DATABASE_URL="your-connection-string"
```

### **Issue: Railway deployment fails**
1. Check Railway logs for errors
2. Verify `Dockerfile` is correct
3. Check environment variables in Railway dashboard
4. Redeploy: Deployments → ... → Redeploy

### **Issue: File upload fails**
- Check file size < 10MB
- Check browser console for errors (F12)
- Check Railway logs for server errors

### **Issue: DateTime errors**
- Ensure all DateTime uses `DateTime.UtcNow` not `DateTime.Now`
- Check `ShipmentDate` parsing in PDF parser

---

## 📂 **Important Files**

| File | Purpose |
|------|---------|
| `Program.cs` | App configuration, middleware |
| `Pages/Login.cshtml.cs` | Login logic |
| `Pages/OfferProcessor.razor` | Main processing page |
| `Services/OfferProcessingService.cs` | Business logic |
| `Dockerfile` | Docker build instructions |
| `railway.toml` | Railway configuration |
| `run-local.ps1` | Local development script |

---

## 🔧 **Configuration**

### **Environment Variables**

**Local (run-local.ps1):**
```powershell
$env:DATABASE_URL="postgresql://user:pass@host/db?sslmode=require"
```

**Railway (Dashboard → Variables):**
- `DATABASE_URL` → Reference from Postgres service
- `ASPNETCORE_ENVIRONMENT` → Production
- `PORT` → Auto-set by Railway

### **Ports**
- **Local:** 5000
- **Railway:** 8080 (auto-configured)

---

## 📊 **Database**

### **Neon.tech (Local Development)**
```
Provider: PostgreSQL 15
Region: Singapore
Free Tier: 0.5GB storage
SSL: Required
```

### **Railway Postgres (Production)**
```
Provider: PostgreSQL
Auto-managed by Railway
SSL: Required
```

### **Connection String Format**
```
postgresql://user:password@host:port/database?sslmode=require
```

---

## 🎯 **Testing Checklist**

### **Before Deployment**
- [ ] Test login/logout locally
- [ ] Test file upload locally
- [ ] Test PDF processing locally
- [ ] Check all DateTime fields use UTC
- [ ] Verify no hardcoded connection strings
- [ ] Run `dotnet build` successfully

### **After Deployment**
- [ ] Access production URL
- [ ] Test login with default credentials
- [ ] Test file upload
- [ ] Check Railway logs for errors
- [ ] Verify database connection
- [ ] Test logout

---

## 🚨 **Emergency Procedures**

### **App Crashed on Railway**
1. Check logs: Dashboard → Service → Deployments → View logs
2. Look for error messages
3. Check database connection
4. Redeploy if needed
5. Contact support if persistent

### **Database Issues**
1. Check Railway Postgres service status
2. Verify `DATABASE_URL` variable is set
3. Check connection string format
4. Test connection from local machine
5. Consider using Neon.tech as backup

### **Rollback Deployment**
```powershell
# In Railway Dashboard:
# Deployments → Find working deployment → ... → Redeploy
```

---

## 📝 **Maintenance Tasks**

### **Daily**
- Check Railway logs for errors
- Monitor app performance

### **Weekly**
- Review user activity
- Check database size
- Update dependencies if needed

### **Monthly**
- Backup database
- Review and rotate logs
- Update documentation

---

## 🔗 **Useful Resources**

- [Railway Docs](https://docs.railway.app/)
- [Neon.tech Docs](https://neon.tech/docs)
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core/)
- [Blazor Docs](https://docs.microsoft.com/aspnet/core/blazor/)
- [PostgreSQL Docs](https://www.postgresql.org/docs/)

---

## 💡 **Tips & Tricks**

1. **Fast Local Testing:**
   - Use `dotnet watch run` for auto-reload
   - Keep Railway logs open in separate window

2. **Debugging:**
   - Use browser DevTools (F12) for client-side issues
   - Check Railway logs for server-side issues
   - Add `Console.WriteLine()` for quick debugging

3. **Performance:**
   - Railway free tier has limits
   - Optimize PDF parsing
   - Use caching where possible

4. **Security:**
   - Never commit `.env` files
   - Change default password immediately
   - Use strong passwords
   - Keep dependencies updated

---

**Last Updated:** May 3, 2026
