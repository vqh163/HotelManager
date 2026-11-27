using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối CSDL SQL Server (Entity Framework Core Code-First)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Cookie Authentication tự code (KHÔNG dùng Identity mặc định)
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
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

    // Kiểm tra trạng thái tài khoản thực tế trong CSDL để lập tức thu hồi phiên nếu tài khoản bị Khóa/Xóa/Đổi quyền
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            try
            {
                var maTaiKhoan = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var vaiTroCookie = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrWhiteSpace(maTaiKhoan))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var taiKhoan = await db.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan);

                if (taiKhoan == null
                    || !string.Equals(taiKhoan.TrangThai?.Trim(), "Hoạt động", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals((taiKhoan.VaiTro ?? HangSoHeThong.VaiTroKhachHang).Trim(), (vaiTroCookie ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
            catch
            {
                // Giữ an toàn nếu có lỗi kết nối tạm thời
            }
        }
    };
});

// 3. Kích hoạt Session phục vụ OTP và luồng xác thực
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 4. Cấu hình Phân quyền tập trung theo Chính sách (Authorization Policies) cho toàn bộ thư mục/trang nội bộ
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(HangSoHeThong.PolicyQuanTri, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroQuanTri, HangSoHeThong.VaiTroQuanTriCu))
    .AddPolicy(HangSoHeThong.PolicyQuanLyCap, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicyNhanSu, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc, HangSoHeThong.VaiTroQuanTri, HangSoHeThong.VaiTroQuanTriCu))
    .AddPolicy(HangSoHeThong.PolicyNoiBo, policy =>
        policy.RequireRole(
            HangSoHeThong.VaiTroQuanTri, HangSoHeThong.VaiTroQuanTriCu,
            HangSoHeThong.VaiTroGiamDoc, HangSoHeThong.VaiTroQuanLy,
            HangSoHeThong.VaiTroLeTan, HangSoHeThong.VaiTroBuongPhong, HangSoHeThong.VaiTroBuongPhongCu,
            HangSoHeThong.VaiTroKyThuat, HangSoHeThong.VaiTroKyThuatCu,
            HangSoHeThong.VaiTroThuNgan, HangSoHeThong.VaiTroPhucVu, HangSoHeThong.VaiTroDauBep,
            HangSoHeThong.VaiTroThuKho, HangSoHeThong.VaiTroSales, HangSoHeThong.VaiTroKeToan))
    .AddPolicy(HangSoHeThong.PolicyKho, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroThuKho, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicyNhaHang, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroPhucVu, HangSoHeThong.VaiTroThuNgan, HangSoHeThong.VaiTroDauBep, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicyThucDon, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroDauBep, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicyThuNgan, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroThuNgan, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicySoDoPhong, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc, HangSoHeThong.VaiTroLeTan, HangSoHeThong.VaiTroSales, HangSoHeThong.VaiTroBuongPhong, HangSoHeThong.VaiTroBuongPhongCu))
    .AddPolicy(HangSoHeThong.PolicyTienSanh, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroLeTan, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicySuCo, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc, HangSoHeThong.VaiTroQuanTri, HangSoHeThong.VaiTroLeTan, HangSoHeThong.VaiTroBuongPhong, HangSoHeThong.VaiTroBuongPhongCu, HangSoHeThong.VaiTroKyThuat, HangSoHeThong.VaiTroKyThuatCu))
    .AddPolicy(HangSoHeThong.PolicyBuongPhong, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroBuongPhong, HangSoHeThong.VaiTroBuongPhongCu, HangSoHeThong.VaiTroLeTan, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc))
    .AddPolicy(HangSoHeThong.PolicyKhachHang, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroKhachHang))
    .AddPolicy(HangSoHeThong.PolicyXemAnhKyc, policy =>
        policy.RequireRole(HangSoHeThong.VaiTroLeTan, HangSoHeThong.VaiTroQuanLy, HangSoHeThong.VaiTroGiamDoc, HangSoHeThong.VaiTroQuanTri));

builder.Services.AddRazorPages(options =>
{
    // Khóa toàn bộ các thư mục nội bộ theo nguyên tắc Default-Deny
    options.Conventions.AuthorizeFolder("/QuanTriHeThong", HangSoHeThong.PolicyQuanTri);
    options.Conventions.AuthorizeFolder("/QuanLyNhanSu", HangSoHeThong.PolicyNhanSu);
    options.Conventions.AuthorizeFolder("/QuanLyKho", HangSoHeThong.PolicyKho);
    options.Conventions.AuthorizeFolder("/QuanLyNhaHang", HangSoHeThong.PolicyNhaHang);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/QuanLyOrder", HangSoHeThong.PolicyThucDon);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/QuanLyThucDon", HangSoHeThong.PolicyThucDon);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/ThemMon", HangSoHeThong.PolicyThucDon);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/SuaMon", HangSoHeThong.PolicyThucDon);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/QuayThuNgan", HangSoHeThong.PolicyThuNgan);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/ThemBan", HangSoHeThong.PolicyQuanLyCap);
    options.Conventions.AuthorizePage("/QuanLyNhaHang/SuaBan", HangSoHeThong.PolicyQuanLyCap);
    options.Conventions.AuthorizeFolder("/QuanLyLichCaNhan", HangSoHeThong.PolicyNoiBo);
    options.Conventions.AuthorizeFolder("/QuanLyHoSo", HangSoHeThong.PolicyKhachHang);
    options.Conventions.AuthorizeFolder("/QuanLyTienSanh", HangSoHeThong.PolicyTienSanh);
    options.Conventions.AuthorizeFolder("/QuanLySuCo", HangSoHeThong.PolicySuCo);

    // Phân quyền chi tiết cho thư mục Quản lý Sơ đồ phòng & Đặt phòng
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/QuanLyDSPhong", HangSoHeThong.PolicyQuanLyCap);
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/ThemPhong", HangSoHeThong.PolicyQuanLyCap);
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/SuaPhong", HangSoHeThong.PolicyQuanLyCap);
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/CapNhatTrangThai", HangSoHeThong.PolicyBuongPhong);
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/SoDoPhong", HangSoHeThong.PolicySoDoPhong);
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/DatPhong");
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/LichSuDatPhong");
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/XacNhanDatPhong");
    options.Conventions.AuthorizePage("/QuanLySoDoPhong/HoaDonCheckout");
    options.Conventions.AllowAnonymousToPage("/QuanLySoDoPhong/TimPhong");
});

var app = builder.Build();

// 5. Đồng bộ dữ liệu cũ từ các file phẳng (.txt / .json) vào SQL Server bằng LINQ (chạy 1 lần duy nhất)
DongBoDuLieuCuVaoCSDL(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// 6. Middleware Bảo mật: Xác thực trước khi phục vụ tệp tĩnh để bảo vệ thư mục ảnh CCCD/KYC
app.UseAuthentication();

// Chặn truy cập công khai vào ảnh CCCD/Hộ chiếu khách hàng (/uploads/kyc/*)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads/kyc", StringComparison.OrdinalIgnoreCase))
    {
        if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        bool duocPhepXem = context.User.IsInRole(HangSoHeThong.VaiTroLeTan)
            || context.User.IsInRole(HangSoHeThong.VaiTroQuanLy)
            || context.User.IsInRole(HangSoHeThong.VaiTroGiamDoc)
            || context.User.IsInRole(HangSoHeThong.VaiTroQuanTri);

        if (!duocPhepXem)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }
    await next();
});

app.UseStaticFiles();
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();
app.Run();

// =========================================================================
// HÀM ĐỒNG BỘ DỮ LIỆU CŨ TỪ TỆP PHẲNG (.txt, .json) VÀO SQL SERVER BẰNG LINQ
// =========================================================================
static void DongBoDuLieuCuVaoCSDL(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (!db.Database.CanConnect()) return;

        // A. Đồng bộ nhanvien_meta.txt -> NHAN_VIEN (CHUC_VUMaChucVu, LuongCoBan)
        string nvMetaPath = Path.Combine(env.ContentRootPath, "nhanvien_meta.txt");
        if (File.Exists(nvMetaPath))
        {
            var chucVus = db.CHUC_VU.AsNoTracking().ToList();
            var nhanViens = db.NHAN_VIEN.ToList();
            var lines = File.ReadAllLines(nvMetaPath, Encoding.UTF8);

            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                var parts = rawLine.Split('|');
                if (parts.Length < 2) continue;

                string maNV = parts[0].Trim();
                string tenChucVu = SuaLoiMaHoaTiengViet(parts[1].Trim());
                var nv = nhanViens.FirstOrDefault(n => n.MaNV == maNV);
                if (nv == null) continue;

                if (string.IsNullOrWhiteSpace(nv.CHUC_VUMaChucVu))
                {
                    var cv = chucVus.FirstOrDefault(c =>
                        string.Equals(c.TenChucVu?.Trim(), tenChucVu, StringComparison.OrdinalIgnoreCase));
                    if (cv != null) nv.CHUC_VUMaChucVu = cv.MaChucVu;
                }

                if (!nv.LuongCoBan.HasValue && parts.Length >= 3 &&
                    decimal.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal luong))
                {
                    nv.LuongCoBan = luong;
                }
            }
            db.SaveChanges();
            File.Move(nvMetaPath, nvMetaPath + ".imported", overwrite: true);
        }

        // B. Đồng bộ menu_data.json -> DICH_VU (MoTa, CanhBaoDiUng, ConMon)
        string menuJsonPath = Path.Combine(env.ContentRootPath, "menu_data.json");
        if (File.Exists(menuJsonPath))
        {
            var json = File.ReadAllText(menuJsonPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json);
            var dichVus = db.DICH_VU.ToList();

            if (doc.RootElement.TryGetProperty("DuLieu", out var duLieuProp))
            {
                foreach (var kv in duLieuProp.EnumerateObject())
                {
                    var dv = dichVus.FirstOrDefault(d => d.MaDichVu == kv.Name);
                    if (dv != null && kv.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        dv.ConMon = kv.Value.GetBoolean();
                }
            }
            if (doc.RootElement.TryGetProperty("MoTaDict", out var moTaProp))
            {
                foreach (var kv in moTaProp.EnumerateObject())
                {
                    var dv = dichVus.FirstOrDefault(d => d.MaDichVu == kv.Name);
                    if (dv != null && string.IsNullOrWhiteSpace(dv.MoTa))
                        dv.MoTa = kv.Value.GetString();
                }
            }
            if (doc.RootElement.TryGetProperty("DiUngDict", out var diUngProp))
            {
                foreach (var kv in diUngProp.EnumerateObject())
                {
                    var dv = dichVus.FirstOrDefault(d => d.MaDichVu == kv.Name);
                    if (dv != null && string.IsNullOrWhiteSpace(dv.CanhBaoDiUng))
                        dv.CanhBaoDiUng = kv.Value.GetString();
                }
            }
            db.SaveChanges();
            File.Move(menuJsonPath, menuJsonPath + ".imported", overwrite: true);
        }

        // C. Đồng bộ lichsu_nhapxuat_meta.txt -> PHIEU_NHAP_XUAT_KHO
        string nhapXuatPath = Path.Combine(env.ContentRootPath, "lichsu_nhapxuat_meta.txt");
        if (File.Exists(nhapXuatPath))
        {
            var lines = File.ReadAllLines(nhapXuatPath, Encoding.UTF8);
            foreach (var rawLine in lines)
            {
                var p = rawLine.Split('|');
                if (p.Length < 11) continue;
                int.TryParse(p[6], out int sl);
                decimal.TryParse(p[7], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal gia);
                DateTime.TryParse(p[10], out DateTime tg);

                db.PHIEU_NHAP_XUAT_KHO.Add(new PHIEU_NHAP_XUAT_KHO
                {
                    MaPhieu = p[0].Trim(),
                    LoaiGiaoDich = p[1].Trim(),
                    KHO_VAT_TUMaVatTu = p[2].Trim(),
                    TenVatTu = SuaLoiMaHoaTiengViet(p[3].Trim()),
                    LoaiKho = SuaLoiMaHoaTiengViet(p[4].Trim()),
                    DonViTinh = SuaLoiMaHoaTiengViet(p[5].Trim()),
                    SoLuong = sl,
                    DonGia = gia,
                    DoiTacHoacBoPhan = SuaLoiMaHoaTiengViet(p[8].Trim()),
                    GhiChu = SuaLoiMaHoaTiengViet(p[9].Trim()),
                    ThoiGian = tg == default ? DateTime.Now : tg
                });
            }
            db.SaveChanges();
            File.Move(nhapXuatPath, nhapXuatPath + ".imported", overwrite: true);
        }

        // D. Đồng bộ lichsu_kiemke_meta.txt -> BIEN_BAN_KIEM_KE
        string kiemKePath = Path.Combine(env.ContentRootPath, "lichsu_kiemke_meta.txt");
        if (File.Exists(kiemKePath))
        {
            var maDaCo = db.BIEN_BAN_KIEM_KE.AsNoTracking().Select(b => b.MaBienBan).ToHashSet();
            var lines = File.ReadAllLines(kiemKePath, Encoding.UTF8);
            foreach (var rawLine in lines)
            {
                var p = rawLine.Split('|');
                if (p.Length < 9) continue;
                string maBB = p[0].Trim();
                if (maDaCo.Contains(maBB)) continue;

                int.TryParse(p[4], out int tongMH);
                int.TryParse(p[5], out int soLech);
                decimal.TryParse(p[6], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal giaTri);
                DateTime.TryParse(p[8], out DateTime tg);

                db.BIEN_BAN_KIEM_KE.Add(new BIEN_BAN_KIEM_KE
                {
                    MaBienBan = maBB,
                    KyKiemKe = SuaLoiMaHoaTiengViet(p[1].Trim()),
                    PhamViKho = SuaLoiMaHoaTiengViet(p[2].Trim()),
                    NguoiKiemKe = SuaLoiMaHoaTiengViet(p[3].Trim()),
                    TongMatHang = tongMH,
                    SoMatHangLech = soLech,
                    GiaTriChenhLech = giaTri,
                    GhiChu = SuaLoiMaHoaTiengViet(p[7].Trim()),
                    ThoiGian = tg == default ? DateTime.Now : tg
                });
                maDaCo.Add(maBB);
            }
            db.SaveChanges();
            File.Move(kiemKePath, kiemKePath + ".imported", overwrite: true);
        }

        // E. Trích xuất bổ sung NHAN_VIENMaNV cho các bản ghi LICH_LAM_VIEC cũ chưa có cột MaNV
        var lichChuaCoMaNV = db.LICH_LAM_VIEC
            .Where(l => string.IsNullOrEmpty(l.NHAN_VIENMaNV) && !string.IsNullOrEmpty(l.GhiChu))
            .ToList();
        if (lichChuaCoMaNV.Any())
        {
            foreach (var lich in lichChuaCoMaNV)
            {
                string gc = lich.GhiChu ?? string.Empty;
                int openParen = gc.IndexOf('(');
                int closeParen = openParen >= 0 ? gc.IndexOf(')', openParen) : -1;
                if (openParen >= 0 && closeParen > openParen + 1)
                {
                    string candidate = gc.Substring(openParen + 1, closeParen - openParen - 1).Trim();
                    if (candidate.StartsWith("NV", StringComparison.OrdinalIgnoreCase) && candidate.Length <= 20)
                    {
                        lich.NHAN_VIENMaNV = candidate;
                    }
                }
            }
            db.SaveChanges();
        }

        // F. Chuẩn hóa TrangThaiThanhToan cho các HOA_DON_NHA_HANG cũ
        var hdNhaHangCu = db.HOA_DON_NHA_HANG
            .Where(h => string.IsNullOrEmpty(h.TrangThaiThanhToan))
            .ToList();
        if (hdNhaHangCu.Any())
        {
            foreach (var hd in hdNhaHangCu)
            {
                string httt = (hd.HinhThucThanhToan ?? string.Empty).Trim();
                if (httt.Equals("Đã Thanh Toán", StringComparison.OrdinalIgnoreCase))
                    hd.TrangThaiThanhToan = HangSoHeThong.HoaDonNHDaThanhToan;
                else if (httt.Equals("Chờ Thanh Toán", StringComparison.OrdinalIgnoreCase))
                    hd.TrangThaiThanhToan = HangSoHeThong.HoaDonNHChoThanhToan;
                else
                    hd.TrangThaiThanhToan = HangSoHeThong.HoaDonNHDangPhucVu;
            }
            db.SaveChanges();
        }
    }
    catch
    {
        // Bỏ qua lỗi nếu DB chưa chạy Migration trong lần đầu khởi động
    }
}

static string SuaLoiMaHoaTiengViet(string text)
{
    if (string.IsNullOrEmpty(text)) return text;
    if (text.Contains("Ã") || text.Contains("á»") || text.Contains("Ä"))
    {
        try
        {
            byte[] latinBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
            string fixedText = Encoding.UTF8.GetString(latinBytes);
            if (!fixedText.Contains('\uFFFD')) return fixedText;
        }
        catch { }
    }
    return text;
}
