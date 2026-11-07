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
    public class SuaTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Tiêm ApplicationDbContext và IWebHostEnvironment để đồng bộ dữ liệu DB và file meta của UC_11
        public SuaTaiKhoanModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public class InputModel
        {
            [Required]
            public string MaTaiKhoan { get; set; } = string.Empty;

            // Mã nhân viên sở hữu tài khoản (Bắt buộc đối với tài khoản nhân sự nội bộ)
            public string? MaNhanVien { get; set; }

            // Lưu giữ mã khách hàng nếu đây là tài khoản của Khách hàng (UC_02)
            public string? MaKhachHang { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
            [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
            public string TenDangNhap { get; set; } = string.Empty;

            // Mật khẩu mới: Không bắt buộc (Để trống nếu giữ nguyên mật khẩu cũ)
            [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên.")]
            public string? MatKhauMoi { get; set; }

            [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới!")]
            public string? XacNhanMatKhauMoi { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
            public string VaiTro { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
            public string TrangThai { get; set; } = "Hoạt động";

            // Các thuộc tính dùng để hiển thị thông tin cá nhân trên Form (Readonly)
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

        // Thông báo thành công lưu qua TempData
        [TempData]
        public string? ThongBaoThanhCong { get; set; }

        // Danh sách Dropdown các nhân viên khả dụng (Chưa có TK + Nhân viên hiện tại của TK này)
        public SelectList DanhSachNhanVienKhaDung { get; set; } = default!;

        // Danh sách Dropdown Vai trò hệ thống
        public SelectList DanhSachVaiTro { get; set; } = default!;

        // Hàm phụ trợ: Đọc chức vụ thực tế của nhân viên từ CSDL (bảng CHUC_VU) và file nhanvien_meta.txt
        private async Task<string> LayChucVuThucTeCuaNhanVienAsync(NHAN_VIEN nv)
        {
            string tenChucVu = string.Empty;

            if (!string.IsNullOrWhiteSpace(nv.CHUC_VUMaChucVu))
            {
                // 1. Kiểm tra theo MaChucVu đã HasData trong ApplicationDbContext (VD: "CV03")
                var chucVuTheoMa = await _context.CHUC_VU
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.MaChucVu == nv.CHUC_VUMaChucVu);

                if (chucVuTheoMa != null && !string.IsNullOrWhiteSpace(chucVuTheoMa.TenChucVu))
                {
                    tenChucVu = chucVuTheoMa.TenChucVu.Trim();
                }
                else
                {
                    // 2. Kiểm tra theo TenChucVu trong bảng CHUC_VU
                    var chucVuTheoTen = await _context.CHUC_VU
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.TenChucVu.ToLower() == nv.CHUC_VUMaChucVu.ToLower());

                    if (chucVuTheoTen != null && !string.IsNullOrWhiteSpace(chucVuTheoTen.TenChucVu))
                    {
                        tenChucVu = chucVuTheoTen.TenChucVu.Trim();
                    }
                    else
                    {
                        // 3. Lấy trực tiếp chuỗi đang lưu trong cột CHUC_VUMaChucVu
                        tenChucVu = nv.CHUC_VUMaChucVu.Trim();
                    }
                }
            }

            // 4. Fallback đọc từ file nhanvien_meta.txt nếu cột trong DB đang trống
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

        // Hàm phụ trợ: Nạp dữ liệu cho các Dropdown (Bao gồm cả nhân viên hiện tại của tài khoản đang sửa)
        private async Task LoadDropdownDataAsync(string maTaiKhoanHienTai, string? maNhanVienDangChon)
        {
            // 1. Lấy danh sách mã nhân viên ĐÃ được cấp cho CÁC TÀI KHOẢN KHÁC (loại trừ tài khoản đang sửa)
            var maNVDaCoTK = await _context.TAI_KHOAN
                .AsNoTracking()
                .Where(t => t.MaTaiKhoan != maTaiKhoanHienTai && t.NHAN_VIENMaNV != null && t.NHAN_VIENMaNV != "")
                .Select(t => t.NHAN_VIENMaNV!)
                .Distinct()
                .ToListAsync();

            // 2. Lấy các nhân viên chưa có tài khoản HOẶC chính là nhân viên đang gắn với tài khoản này
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

            DanhSachNhanVienKhaDung = new SelectList(nhanVienHienThi, "MaNV", "HienThi", maNhanVienDangChon);

            // 3. Lấy toàn bộ danh sách chức vụ từ bảng CHUC_VU (đã được seed sẵn trong ApplicationDbContext)
            var danhSachChucVuDb = await _context.CHUC_VU
                .AsNoTracking()
                .Where(c => c.TenChucVu != null && c.TenChucVu != "")
                .Select(c => c.TenChucVu!.Trim())
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            DanhSachVaiTro = new SelectList(danhSachChucVuDb, Input.VaiTro);
        }

        // Hàm phụ trợ: Nạp thông tin cá nhân của Nhân viên (hoặc Khách hàng) vào InputModel để hiển thị Readonly
        private async Task NapThongTinCaNhanHienThiAsync(string? maNV, string? maKH)
        {
            if (!string.IsNullOrWhiteSpace(maNV))
            {
                var nv = await _context.NHAN_VIEN.AsNoTracking().FirstOrDefaultAsync(x => x.MaNV == maNV);
                if (nv != null)
                {
                    Input.HoTen = nv.HoTen;
                    Input.SoCCCD = nv.SoCCCD;
                    Input.SDT = nv.SDT;
                    Input.Email = nv.Email;
                    Input.TenChucVu = await LayChucVuThucTeCuaNhanVienAsync(nv);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(maKH))
            {
                var kh = await _context.KHACH_HANG.AsNoTracking().FirstOrDefaultAsync(x => x.MaKH == maKH);
                if (kh != null)
                {
                    Input.HoTen = kh.HoTen;
                    Input.SoCCCD = kh.SoCCCD;
                    Input.SDT = kh.SDT;
                    Input.Email = kh.Email;
                    Input.TenChucVu = $"Khách hàng ({kh.HangThanhVien ?? "Thành viên"})";
                }
            }
        }

        // Hàm phụ trợ: Băm mật khẩu chuẩn SHA-256 sang chuỗi Base64 (44 ký tự, vừa khít VARCHAR(50))
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // Hỗ trợ nhận tham số từ cả asp-route-id hoặc asp-route-maTaiKhoan
        public async Task<IActionResult> OnGetAsync(string? id, string? maTaiKhoan)
        {
            string targetId = !string.IsNullOrWhiteSpace(id) ? id : (maTaiKhoan ?? string.Empty);
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            var taiKhoan = await _context.TAI_KHOAN
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == targetId);

            if (taiKhoan == null)
            {
                return NotFound("Không tìm thấy tài khoản cần chỉnh sửa.");
            }

            // Đổ dữ liệu hiện tại của tài khoản lên Form
            Input = new InputModel
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TenDangNhap = taiKhoan.TenDangNhap ?? string.Empty,
                VaiTro = taiKhoan.VaiTro ?? string.Empty,
                TrangThai = taiKhoan.TrangThai ?? "Hoạt động",
                MaNhanVien = taiKhoan.NHAN_VIENMaNV,
                MaKhachHang = taiKhoan.KHACH_HANGMaKH
            };

            // Tải thông tin cá nhân tương ứng của nhân viên/khách hàng
            await NapThongTinCaNhanHienThiAsync(Input.MaNhanVien, Input.MaKhachHang);

            // Tải danh sách Dropdown
            await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);

            return Page();
        }

        // Handler AJAX trả về JSON thông tin chi tiết của nhân viên khi Admin đổi nhân viên trên Dropdown
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
            // Nạp lại thông tin cá nhân để không bị mất trên giao diện nếu báo lỗi Validation
            await NapThongTinCaNhanHienThiAsync(Input.MaNhanVien, Input.MaKhachHang);

            // Nếu Admin để trống ô Mật khẩu mới (không muốn đổi mật khẩu), bỏ qua kiểm tra độ dài mật khẩu
            if (string.IsNullOrWhiteSpace(Input.MatKhauMoi))
            {
                ModelState.Remove("Input.MatKhauMoi");
                ModelState.Remove("Input.XacNhanMatKhauMoi");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);
                return Page();
            }

            try
            {
                var taiKhoanDb = await _context.TAI_KHOAN
                    .FirstOrDefaultAsync(t => t.MaTaiKhoan == Input.MaTaiKhoan);

                if (taiKhoanDb == null)
                {
                    return NotFound("Tài khoản không còn tồn tại trong hệ thống.");
                }

                string tenDangNhapChuan = Input.TenDangNhap.Trim();

                // 1. Kiểm tra trùng Tên đăng nhập với các tài khoản KHÁC (loại trừ chính tài khoản đang sửa)
                bool trungTenDangNhap = await _context.TAI_KHOAN
                    .AnyAsync(t => t.MaTaiKhoan != Input.MaTaiKhoan && t.TenDangNhap.ToLower() == tenDangNhapChuan.ToLower());

                if (trungTenDangNhap)
                {
                    ThongBaoLoi = $"Tên đăng nhập '{tenDangNhapChuan}' đã được sử dụng bởi một tài khoản khác!";
                    ModelState.AddModelError("Input.TenDangNhap", "Tên đăng nhập đã bị trùng.");
                    await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);
                    return Page();
                }

                // 2. Nếu có chọn Mã nhân viên, kiểm tra nhân viên tồn tại và chưa bị gắn cho tài khoản khác
                NHAN_VIEN? nhanVien = null;
                if (!string.IsNullOrWhiteSpace(Input.MaNhanVien))
                {
                    nhanVien = await _context.NHAN_VIEN
                        .FirstOrDefaultAsync(nv => nv.MaNV == Input.MaNhanVien);

                    if (nhanVien == null)
                    {
                        ThongBaoLoi = "Mã nhân viên được chọn không tồn tại trong dữ liệu Quản lý Nhân sự!";
                        await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);
                        return Page();
                    }

                    bool trungNhanVien = await _context.TAI_KHOAN
                        .AnyAsync(t => t.MaTaiKhoan != Input.MaTaiKhoan && t.NHAN_VIENMaNV == Input.MaNhanVien);

                    if (trungNhanVien)
                    {
                        ThongBaoLoi = $"Nhân viên {nhanVien.HoTen} ({Input.MaNhanVien}) đã được gắn với một tài khoản khác!";
                        await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);
                        return Page();
                    }

                    // Đồng bộ lại chức vụ của nhân viên trong bảng NHAN_VIEN theo VaiTro mới (nếu có thay đổi)
                    if (!string.IsNullOrWhiteSpace(Input.VaiTro))
                    {
                        var chucVuDb = await _context.CHUC_VU
                            .FirstOrDefaultAsync(c => c.TenChucVu.ToLower() == Input.VaiTro.Trim().ToLower());

                        if (chucVuDb != null)
                        {
                            nhanVien.CHUC_VUMaChucVu = chucVuDb.MaChucVu;
                            _context.NHAN_VIEN.Update(nhanVien);
                        }
                    }
                }

                // 3. Cập nhật thông tin vào thực thể TAI_KHOAN
                string vaiTroCu = taiKhoanDb.VaiTro ?? "";
                string trangThaiCu = taiKhoanDb.TrangThai ?? "";
                bool daDoiMatKhau = false;

                taiKhoanDb.TenDangNhap = tenDangNhapChuan;
                taiKhoanDb.VaiTro = Input.VaiTro;
                taiKhoanDb.TrangThai = Input.TrangThai;
                taiKhoanDb.NHAN_VIENMaNV = string.IsNullOrWhiteSpace(Input.MaNhanVien) ? null : Input.MaNhanVien;

                // Chỉ cập nhật và băm mật khẩu mới nếu Admin có nhập vào ô Mật khẩu mới
                if (!string.IsNullOrWhiteSpace(Input.MatKhauMoi))
                {
                    taiKhoanDb.MatKhau = HashPassword(Input.MatKhauMoi);
                    daDoiMatKhau = true;
                }

                _context.TAI_KHOAN.Update(taiKhoanDb);

                // 4. Lưu vết thao tác (Audit Trail) vào bảng NHAT_KY_HE_THONG (UC_13)
                string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                string ghiChuMatKhau = daDoiMatKhau ? ", Đã đặt lại mật khẩu mới (SHA-256)" : ", Giữ nguyên mật khẩu cũ";

                var nhatKy = new NHAT_KY_HE_THONG
                {
                    MaNhatKy = $"LG{timestamp}{randomSuffix}",
                    TenDangNhap = User.Identity?.Name ?? "Admin",
                    HanhDong = "Cập nhật tài khoản",
                    ChiTiet = $"Sửa TK '{tenDangNhapChuan}' ({Input.MaTaiKhoan}): Vai trò [{vaiTroCu} -> {Input.VaiTro}], Trạng thái [{trangThaiCu} -> {Input.TrangThai}]{ghiChuMatKhau}"
                };
                _context.NHAT_KY_HE_THONG.Add(nhatKy);

                await _context.SaveChangesAsync();

                ThongBaoThanhCong = $"Cập nhật thông tin tài khoản '{tenDangNhapChuan}' ({Input.MaTaiKhoan}) thành công!";

                // Tải lại thông tin hiển thị sau khi cập nhật thành công
                await NapThongTinCaNhanHienThiAsync(Input.MaNhanVien, Input.MaKhachHang);
                await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi cập nhật CSDL: " + ex.Message;
                await LoadDropdownDataAsync(Input.MaTaiKhoan, Input.MaNhanVien);
                return Page();
            }
        }
    }
}