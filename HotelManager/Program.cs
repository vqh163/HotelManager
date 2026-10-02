using HotelManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Không tìm thấy chuỗi kết nối DefaultConnection.");

// Đăng ký ApplicationDbContext vào hệ thống Dependency Injection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddRazorPages();
// 1. CẤU HÌNH COOKIE AUTHENTICATION VÀ ĐẶT LÀM MẶC ĐỊNH CHO TOÀN HỆ THỐNG
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Dòng này sẽ sửa triệt để lỗi màu đỏ của bạn
})
.AddCookie(options =>
{
    // Cấu hình đường dẫn điều hướng thông minh
    options.LoginPath = "/Auth/DangNhap"; // Trục xuất về trang này nếu chưa đăng nhập
    options.AccessDeniedPath = "/Index";  // Trục xuất về trang chủ nếu ĐÃ đăng nhập nhưng SAI QUYỀN (VD: Lễ tân cố vào trang Admin)
});

// Kích hoạt Session (Hỗ trợ luồng Quên mật khẩu UC_01 mà bạn Trần làm)
builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
