using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class ThemTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Tiêm cả ApplicationDbContext và IWebHostEnvironment để đồng bộ dữ liệu từ DB và file meta của UC_11
        public ThemTaiKhoanModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng chọn nhân viên cần cấp tài khoản.")]
            public string MaNhanVien { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
            [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
            public string MatKhau { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu.")]
            [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập!")]
            public string XacNhanMatKhau { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
            public string VaiTro { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
            public string TrangThai { get; set; } = "Hoạt động";

            // Các thuộc tính dùng để hiển thị thông tin cá nhân của nhân viên trên Form
            public string? HoTen { get; set; }
            public string? SoCCCD { get; set; }
            public string? SDT { get; set; }
            public string? Email { get; set; }
            public string? TenChucVu { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        // Thông báo lỗi hiển thị trên giao diện
        public string ThongBaoLoi { get; set; } = string.Empty;

        // Thông báo thành công hiển thị trên giao diện
        [TempData]
        public string? ThongBaoThanhCong { get; set; }

        // Danh sách Dropdown các nhân viên CHƯA CÓ tài khoản
        public SelectList DanhSachNhanVienChuaCoTK { get; set; } = default!;

        // Danh sách Dropdown Vai trò hệ thống
        public SelectList DanhSachVaiTro { get; set; } = default!;

        // Hàm phụ trợ: Đọc chức vụ của nhân viên từ cả Database (bảng CHUC_VU, NHAN_VIEN) và file nhanvien_meta.txt của UC_11
        // Hàm phụ trợ: Đọc chức vụ thực tế của nhân viên từ CSDL (Ưu tiên bảng CHUC_VU qua khóa ngoại CHUC_VUMaChucVu)
        private async Task<string> LayChucVuThucTeCuaNhanVienAsync(NHAN_VIEN nv)
        {
            string tenChucVu = string.Empty;

            if (!string.IsNullOrWhiteSpace(nv.CHUC_VUMaChucVu))
            {
                // 1. Kiểm tra xem giá trị lưu trong NHAN_VIEN.CHUC_VUMaChucVu có khớp với MaChucVu đã HasData không (VD: "CV03")
                var chucVuTheoMa = await _context.CHUC_VU
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.MaChucVu == nv.CHUC_VUMaChucVu);

                if (chucVuTheoMa != null && !string.IsNullOrWhiteSpace(chucVuTheoMa.TenChucVu))
                {
                    tenChucVu = chucVuTheoMa.TenChucVu.Trim();
                }
                else
                {
                    // 2. Nếu không khớp Mã, tìm theo TenChucVu trong bảng CHUC_VU
                    var chucVuTheoTen = await _context.CHUC_VU
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.TenChucVu.ToLower() == nv.CHUC_VUMaChucVu.ToLower());

                    if (chucVuTheoTen != null && !string.IsNullOrWhiteSpace(chucVuTheoTen.TenChucVu))
                    {
                        tenChucVu = chucVuTheoTen.TenChucVu.Trim();
                    }
                    else
                    {
                        // 3. Nếu vẫn không thấy, lấy trực tiếp chuỗi đang lưu trong cột (VD: "Lễ tân")
                        tenChucVu = nv.CHUC_VUMaChucVu.Trim();
                    }
                }
            }

            // 4. Fallback cuối cùng: Đọc từ file nhanvien_meta.txt (nếu module nhân sự của bạn đang lưu file text)
            if (string.IsNullOrWhiteSpace(tenChucVu))
            {
                string metaFilePath = Path.Combine(_env.ContentRootPath, "nhanvien_meta.txt");
                if (System.IO.File.Exists(metaFilePath))
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(metaFilePath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.Contains(nv.MaNV))
                        {
                            var parts = line.Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                tenChucVu = parts.Skip(1).FirstOrDefault(p => !p.Contains("@") && !p.All(char.IsDigit))?.Trim() ?? "";
                                break;
                            }
                        }
                    }
                }
            }

            return tenChucVu;
        }

        // Hàm phụ trợ: Tải dữ liệu cho các ô Dropdown (Đồng bộ trực tiếp từ bảng CHUC_VU đã seed)
        private async Task LoadDropdownDataAsync()
        {
            // 1. Lấy danh sách mã nhân viên ĐÃ được cấp tài khoản trong bảng TAI_KHOAN
            var maNVDaCoTK = await _context.TAI_KHOAN
                .AsNoTracking()
                .Where(t => t.NHAN_VIENMaNV != null && t.NHAN_VIENMaNV != "")
                .Select(t => t.NHAN_VIENMaNV!)
                .Distinct()
                .ToListAsync();

            // 2. Lọc ra các nhân viên chưa có tài khoản
            var dsNhanVien = await _context.NHAN_VIEN
                .AsNoTracking()
                .Where(nv => !maNVDaCoTK.Contains(nv.MaNV))
                .OrderByDescending(nv => nv.MaNV)
                .ToListAsync();

            var nhanVienHienThi = new List<object>();
            foreach (var nv in dsNhanVien)
            {
                string chucVu = await LayChucVuThucTeCuaNhanVienAsync(nv);
                string suffixChucVu = string.IsNullOrWhiteSpace(chucVu) ? "" : $" ({chucVu})";
                nhanVienHienThi.Add(new
                {
                    MaNV = nv.MaNV,
                    HienThi = $"{nv.MaNV} - {nv.HoTen}{suffixChucVu}"
                });
            }

            DanhSachNhanVienChuaCoTK = new SelectList(nhanVienHienThi, "MaNV", "HienThi", Input.MaNhanVien);

            // 3. Lấy toàn bộ danh sách tên chức vụ từ bảng CHUC_VU (đã được seed sẵn trong ApplicationDbContext.cs)
            var danhSachChucVuDb = await _context.CHUC_VU
                .AsNoTracking()
                .Where(c => c.TenChucVu != null && c.TenChucVu != "")
                .Select(c => c.TenChucVu!.Trim())
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            DanhSachVaiTro = new SelectList(danhSachChucVuDb, Input.VaiTro);
        }

        // Hàm phụ trợ: Băm mật khẩu chuẩn SHA-256 sang chuỗi Base64 (44 ký tự, vừa khít VARCHAR(50))
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdownDataAsync();
            return Page();
        }

        // Handler AJAX trả về JSON thông tin chi tiết của nhân viên khi Admin chọn từ Dropdown
        public async Task<JsonResult> OnGetThongTinNhanVienAsync(string maNV)
        {
            if (string.IsNullOrWhiteSpace(maNV))
            {
                return new JsonResult(new { success = false, message = "Mã nhân viên trống." });
            }

            var nv = await _context.NHAN_VIEN
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaNV == maNV);

            if (nv == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy nhân viên trong hệ thống." });
            }

            // Lấy chức vụ thực tế bằng hàm đọc đa tầng (DB + nhanvien_meta.txt)
            string tenChucVu = await LayChucVuThucTeCuaNhanVienAsync(nv);

            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    hoTen = nv.HoTen ?? "",
                    soCCCD = nv.SoCCCD ?? "",
                    sdt = nv.SDT ?? "",
                    email = nv.Email ?? "",
                    tenChucVu = tenChucVu
                }
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Tải lại thông tin nhân viên đã chọn (để giữ lại trên giao diện nếu form báo lỗi)
            if (!string.IsNullOrWhiteSpace(Input.MaNhanVien))
            {
                var nvInfo = await _context.NHAN_VIEN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(nv => nv.MaNV == Input.MaNhanVien);

                if (nvInfo != null)
                {
                    Input.HoTen = nvInfo.HoTen;
                    Input.SoCCCD = nvInfo.SoCCCD;
                    Input.SDT = nvInfo.SDT;
                    Input.Email = nvInfo.Email;
                    Input.TenChucVu = await LayChucVuThucTeCuaNhanVienAsync(nvInfo);
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownDataAsync();
                return Page();
            }

            try
            {
                string tenDangNhapChuan = Input.TenDangNhap.Trim();

                // 1. Kiểm tra trùng Tên đăng nhập trong bảng TAI_KHOAN
                bool trungTenDangNhap = await _context.TAI_KHOAN
                    .AnyAsync(t => t.TenDangNhap.ToLower() == tenDangNhapChuan.ToLower());

                if (trungTenDangNhap)
                {
                    ThongBaoLoi = $"Tên đăng nhập '{tenDangNhapChuan}' đã tồn tại trong hệ thống. Vui lòng chọn tên đăng nhập khác!";
                    ModelState.AddModelError("Input.TenDangNhap", "Tên đăng nhập đã bị trùng.");
                    await LoadDropdownDataAsync();
                    return Page();
                }

                // 2. Kiểm tra xem Mã Nhân Viên có tồn tại trong bảng NHAN_VIEN hay không
                var nhanVien = await _context.NHAN_VIEN
                    .FirstOrDefaultAsync(nv => nv.MaNV == Input.MaNhanVien);

                if (nhanVien == null)
                {
                    ThongBaoLoi = "Mã nhân viên không tồn tại trong dữ liệu Quản lý Nhân sự!";
                    await LoadDropdownDataAsync();
                    return Page();
                }

                // 3. Kiểm tra xem Nhân viên này đã được cấp tài khoản trước đó hay chưa
                bool nhanVienDaCoTK = await _context.TAI_KHOAN
                    .AnyAsync(t => t.NHAN_VIENMaNV == Input.MaNhanVien);

                if (nhanVienDaCoTK)
                {
                    ThongBaoLoi = $"Nhân viên {nhanVien.HoTen} ({Input.MaNhanVien}) đã được cấp tài khoản trước đó!";
                    await LoadDropdownDataAsync();
                    return Page();
                }

                // 4. Tự động đồng bộ chức vụ vào bảng CHUC_VU và NHAN_VIEN nếu trước đó mới chỉ lưu ở file txt
                if (!string.IsNullOrWhiteSpace(Input.VaiTro))
                {
                    var chucVuTonTai = await _context.CHUC_VU
                        .FirstOrDefaultAsync(c => c.TenChucVu.ToLower() == Input.VaiTro.Trim().ToLower());

                    if (chucVuTonTai == null)
                    {
                        string maCVMoi = "CV" + DateTime.Now.ToString("mmss") + Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper();
                        chucVuTonTai = new CHUC_VU
                        {
                            MaChucVu = maCVMoi,
                            TenChucVu = Input.VaiTro.Trim()
                        };
                        _context.CHUC_VU.Add(chucVuTonTai);
                    }

                    if (string.IsNullOrWhiteSpace(nhanVien.CHUC_VUMaChucVu))
                    {
                        nhanVien.CHUC_VUMaChucVu = chucVuTonTai.MaChucVu;
                        _context.NHAN_VIEN.Update(nhanVien);
                    }
                }

                // Sinh hậu tố ngẫu nhiên 4 ký tự để tránh trùng khóa chính
                string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                string timestamp = DateTime.Now.ToString("yyMMddHHmmss");

                // 5. Khởi tạo đối tượng TAI_KHOAN với mật khẩu đã được Hash (UC_13 - Bảo mật)
                var taiKhoanMoi = new TAI_KHOAN
                {
                    MaTaiKhoan = $"TK{timestamp}{randomSuffix}",
                    TenDangNhap = tenDangNhapChuan,
                    MatKhau = HashPassword(Input.MatKhau),
                    VaiTro = Input.VaiTro,
                    TrangThai = Input.TrangThai,
                    NHAN_VIENMaNV = Input.MaNhanVien,
                    KHACH_HANGMaKH = null
                };

                _context.TAI_KHOAN.Add(taiKhoanMoi);

                // 6. Ghi lưu vết thao tác (Audit Trail) vào bảng NHAT_KY_HE_THONG
                var nhatKy = new NHAT_KY_HE_THONG
                {
                    MaNhatKy = $"LG{timestamp}{randomSuffix}",
                    TenDangNhap = User.Identity?.Name ?? "Admin",
                    HanhDong = "Thêm tài khoản nhân sự",
                    ChiTiet = $"Đã cấp tài khoản '{tenDangNhapChuan}' (Vai trò: {Input.VaiTro}) cho nhân viên {nhanVien.HoTen} ({Input.MaNhanVien})",
                    ThoiGian = DateTime.Now
                };
                _context.NHAT_KY_HE_THONG.Add(nhatKy);

                // Lưu đồng thời xuống SQL Server
                await _context.SaveChangesAsync();

                ThongBaoThanhCong = $"Thêm tài khoản '{tenDangNhapChuan}' cho nhân viên {nhanVien.HoTen} ({Input.MaNhanVien}) thành công!";

                ModelState.Clear();
                Input = new InputModel();
                await LoadDropdownDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi lưu CSDL: " + ex.Message;
                await LoadDropdownDataAsync();
                return Page();
            }
        }
    }
}