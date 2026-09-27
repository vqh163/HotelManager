<<<<<<< HEAD
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data; // Namespace chứa ApplicationDbContext của nhóm
=======
using HotelManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm thư viện này ở đầu file

>>>>>>> 5786684d18364ac52d569f4425b8d04b2ba41520

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

// Add services to the container.
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

// 4. KÍCH HOẠT SESSION MIDDLEWARE (Đặt sau UseRouting và trước UseAuthentication/UseAuthorization)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();