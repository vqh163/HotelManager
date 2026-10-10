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

// 2. MIDDLEWARE BẢO MẬT: Bắt buộc nằm giữa UseRouting và MapRazorPages
app.UseAuthentication(); // Bước A: Xác định "Anh là ai?" (Đọc Cookie)
app.UseAuthorization();  // Bước B: Xác định "Anh có quyền không?" (Kiểm tra Role)
app.UseSession();        // Kích hoạt phiên làm việc

app.MapRazorPages();
app.UseStaticFiles();
app.Run();