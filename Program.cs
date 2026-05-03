using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using CBAS.Web.Data;
using CBAS.Web.Services;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var connectionString = GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

static string GetConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var match = Regex.Match(databaseUrl, @"postgres://([^:]+):([^@]+)@([^:]+):(\d+)/(.+)");
        if (match.Success)
        {
            var user = match.Groups[1].Value;
            var password = match.Groups[2].Value;
            var host = match.Groups[3].Value;
            var port = match.Groups[4].Value;
            var database = match.Groups[5].Value;
            
            return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }
    }
    
    return configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=cbas_db;Username=postgres;Password=postgres";
}

builder.Services.AddScoped<IPdfParserService, PdfParserService>();
builder.Services.AddScoped<IOfferProcessingService, OfferProcessingService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var userExists = await db.Users.AnyAsync(u => u.Username == "admin");
    if (!userExists)
    {
        await authService.CreateUserAsync("admin", "admin123", "Administrator", "admin@cbas.local");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
