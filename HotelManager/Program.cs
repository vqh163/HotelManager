using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kết nối CSDL SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// KHAI BÁO COOKIE AUTHENTICATION (CHỈ ĐƯỢC KHAI BÁO 1 LẦN DUY NHẤT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/DangNhap";
    options.AccessDeniedPath = "/Index";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Kích hoạt Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. MIDDLEWARE BẢO MẬT: Bắt buộc nằm giữa UseRouting và MapRazorPages
app.UseAuthentication(); // Bước A: Xác định "Anh là ai?" (Đọc Cookie)
app.UseAuthorization();  // Bước B: Xác định "Anh có quyền không?" (Kiểm tra Role)
app.UseSession();        // Kích hoạt phiên làm việc

app.MapRazorPages();

app.Run();