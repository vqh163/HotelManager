using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối CSDL SQL Server (Entity Framework Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Cookie Authentication (Tự code, không dùng Identity)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/DangNhap";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// 3. CẤU HÌNH SESSION (BẮT BUỘC PHẢI CÓ ĐỂ CHẠY ĐƯỢC OTP)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. CẤU HÌNH PHÂN QUYỀN (BẮT BUỘC ĐỂ KHÓA CÁC TRANG NỘI BỘ)
// Vai trò lưu trong TAI_KHOAN.VaiTro ("Quản trị viên", "Quản lý", "Lễ tân"...)
// được đưa vào Claim Role khi đăng nhập (xem DangNhap.cshtml.cs).

builder.Services.AddRazorPages(options =>
{
    // Toàn bộ thư mục Quản trị hệ thống chỉ cho phép vai trò "Quản trị viên".
    options.Conventions.AuthorizeFolder("/QuanTriHeThong", "QuanTriVienOnly");
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("QuanTriVienOnly", policy => policy.RequireRole("Quản trị viên"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();