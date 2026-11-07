using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class DatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DatPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PHONG PhongDuocChon { get; set; } = new PHONG();

        [BindProperty]
        public DAT_PHONG ThongTinDatPhong { get; set; } = new DAT_PHONG();

        // BỔ SUNG: 3 biến hứng dữ liệu thông tin cá nhân trên form
        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string HoTenKhach { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại bắt buộc phải đúng 10 chữ số.")]
        public string SDTKhach { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string EmailKhach { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string id, DateTime? ngayNhan, DateTime? ngayTra, int? soKhach)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("/QuanLySoDoPhong/TimPhong");

            try
            {
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();
                PhongDuocChon = phong;

                // CHỈ tự động lấy thông tin cá nhân nếu khách ĐÃ ĐĂNG NHẬP
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var tenDangNhap = User.Identity.Name;
                    var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                    if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                    {
                        var khachHang = await _context.KHACH_HANG.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);
                        if (khachHang != null)
                        {
                            HoTenKhach = khachHang.HoTen ?? "";
                            SDTKhach = khachHang.SDT ?? "";
                            EmailKhach = khachHang.Email ?? "";
                        }
                    }
                }

                if (ngayNhan.HasValue && ngayTra.HasValue)
                {
                    ThongTinDatPhong.NgayNhan = ngayNhan.Value.Date.AddHours(14);
                    ThongTinDatPhong.NgayTra = ngayTra.Value.Date.AddHours(12);
                }
                else
                {
                    ThongTinDatPhong.NgayNhan = DateTime.Now.Date.AddHours(14);
                    ThongTinDatPhong.NgayTra = DateTime.Now.Date.AddDays(1).AddHours(12);
                }

                ThongTinDatPhong.SoLuongKhach = soKhach ?? 1;

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi truy xuất dữ liệu: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            // Chặn người dùng chưa đăng nhập
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToPage("/Auth/DangNhap");

            // Bỏ qua validation các trường hệ thống tự sinh
            ModelState.Remove("ThongTinDatPhong.MaDatPhong");
            ModelState.Remove("ThongTinDatPhong.TrangThaiDatPhong");
            ModelState.Remove("ThongTinDatPhong.KenhDatPhong");
            ModelState.Remove("ThongTinDatPhong.KHACH_HANGMaKH");
            ModelState.Remove("ThongTinDatPhong.NgayDat");

            // Hàm phụ trợ tải lại thông tin phòng khi có lỗi Validation để không bị trắng cột trái trên View
            async Task LoadRoomInfoAsync()
            {
                var phongLoadLai = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phongLoadLai != null) PhongDuocChon = phongLoadLai;
            }

            if (!ModelState.IsValid)
            {
                await LoadRoomInfoAsync();
                ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra và điền đầy đủ các thông tin bắt buộc!");
                return Page();
            }

            if (!ThongTinDatPhong.NgayNhan.HasValue || !ThongTinDatPhong.NgayTra.HasValue)
            {
                await LoadRoomInfoAsync();
                ModelState.AddModelError(string.Empty, "Vui lòng chọn đầy đủ Ngày nhận và Ngày trả phòng.");
                return Page();
            }

            DateTime requestedIn = ThongTinDatPhong.NgayNhan.Value.Date.AddHours(14);
            DateTime requestedOut = ThongTinDatPhong.NgayTra.Value.Date.AddHours(12);
            DateTime today = DateTime.Now.Date;

            // Kiểm tra logic ngày tháng ở Backend (Chống F12 Bypass)
            if (requestedIn.Date < today)
            {
                await LoadRoomInfoAsync();
                ModelState.AddModelError(string.Empty, "Lỗi: Ngày nhận phòng không được chọn trong quá khứ.");
                return Page();
            }

            if (requestedOut.Date <= requestedIn.Date)
            {
                await LoadRoomInfoAsync();
                ModelState.AddModelError(string.Empty, "Lỗi: Ngày trả phòng phải lớn hơn ngày nhận phòng ít nhất 1 ngày.");
                return Page();
            }

            // BẮT ĐẦU TRANSACTION NGUYÊN TỬ (Đảm bảo tính toàn vẹn dữ liệu & chống Double-Booking)
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();

                // Chặn đặt phòng nếu phòng đang bị Khóa hoặc Bảo trì (TT04)
                if (phong.MaTrangThai == "TT04")
                {
                    await LoadRoomInfoAsync();
                    ModelState.AddModelError(string.Empty, "Phòng này hiện đang được bảo trì hoặc bị khóa. Vui lòng chọn phòng khác.");
                    return Page();
                }

                // Kiểm tra Overlap Date ngay bên trong Transaction
                bool isDoubleBooked = await (from dp in _context.DAT_PHONG
                                             join ct in _context.CHI_TIET_DAT_PHONG on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                             where ct.PHONGMaPhong == phong.MaPhong
                                                && dp.TrangThaiDatPhong != "Đã hủy"
                                                && dp.TrangThaiDatPhong != "Đã check-out"
                                                && dp.TrangThaiDatPhong != "Đã hoàn tất"
                                                && dp.NgayNhan < requestedOut
                                                && dp.NgayTra > requestedIn
                                             select dp.MaDatPhong).AnyAsync();

                if (isDoubleBooked)
                {
                    await LoadRoomInfoAsync();
                    ModelState.AddModelError(string.Empty, "Phòng này vừa có người đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác.");
                    return Page();
                }

                // Sinh chuỗi ngẫu nhiên 6 ký tự bảo mật (Chống trùng lặp PK và chống đoán mã đặt phòng)
                string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                string timestampPrefix = DateTime.Now.ToString("yyMMdd");

                // Đồng bộ hồ sơ khách hàng O2O (UC_03 & UC_05)
                string maKhachHangChot = string.Empty;
                var tenDangNhap = User.Identity.Name;
                var taiKhoanDangNhap = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);
                bool isStaffBooking = taiKhoanDangNhap != null && (taiKhoanDangNhap.VaiTro == "Lễ tân" || taiKhoanDangNhap.VaiTro == "Quản lý" || taiKhoanDangNhap.VaiTro == "Sales");

                if (isStaffBooking)
                {
                    var khachHangCu = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.SDT == SDTKhach);
                    if (khachHangCu != null)
                    {
                        maKhachHangChot = khachHangCu.MaKH;
                        khachHangCu.HoTen = HoTenKhach;
                        khachHangCu.Email = EmailKhach;
                        _context.KHACH_HANG.Update(khachHangCu);
                    }
                    else
                    {
                        maKhachHangChot = $"KH{timestampPrefix}{randomSuffix}";
                        var khachHangMoi = new KHACH_HANG
                        {
                            MaKH = maKhachHangChot,
                            HoTen = HoTenKhach,
                            SDT = SDTKhach,
                            Email = EmailKhach,
                            HangThanhVien = "Thành viên mới",
                            DiemThuong = 0
                        };
                        _context.KHACH_HANG.Add(khachHangMoi);
                    }
                }
                else if (taiKhoanDangNhap != null && !string.IsNullOrEmpty(taiKhoanDangNhap.KHACH_HANGMaKH))
                {
                    maKhachHangChot = taiKhoanDangNhap.KHACH_HANGMaKH;
                    var khUpdate = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == maKhachHangChot);
                    if (khUpdate != null)
                    {
                        khUpdate.HoTen = HoTenKhach;
                        khUpdate.SDT = SDTKhach;
                        khUpdate.Email = EmailKhach;
                        _context.KHACH_HANG.Update(khUpdate);
                    }
                }

                // Khởi tạo thông tin phiếu đặt phòng với mã khó đoán (Tổng 14 ký tự, vừa khít VARCHAR(20))
                ThongTinDatPhong.MaDatPhong = $"DP{timestampPrefix}{randomSuffix}";
                ThongTinDatPhong.KHACH_HANGMaKH = maKhachHangChot;
                ThongTinDatPhong.NgayDat = DateTime.Now;
                ThongTinDatPhong.NgayNhan = requestedIn;
                ThongTinDatPhong.NgayTra = requestedOut;
                ThongTinDatPhong.TrangThaiDatPhong = isStaffBooking ? "Đã xác nhận" : "Chờ xác nhận";
                ThongTinDatPhong.KenhDatPhong = isStaffBooking ? "Trực tiếp tại quầy" : "Website Trực tuyến";

                int soNgayO = Math.Max(1, (requestedOut.Date - requestedIn.Date).Days);

                _context.DAT_PHONG.Add(ThongTinDatPhong);

                var chiTiet = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = $"CT{timestampPrefix}{randomSuffix}",
                    DAT_PHONGMaDatPhong = ThongTinDatPhong.MaDatPhong,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,
                    SoNgayO = soNgayO
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTiet);

                // CHỈ đổi trạng thái vật lý của phòng sang TT02 nếu ngày nhận phòng là NGAY HÔM NAY
                if (requestedIn.Date == today)
                {
                    phong.MaTrangThai = "TT02";
                    _context.PHONG.Update(phong);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Chốt giao dịch thành công

                return RedirectToPage("./XacNhanDatPhong", new { maDatPhong = ThongTinDatPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Hoàn tác toàn bộ nếu có bất kỳ lỗi nào xảy ra
                await LoadRoomInfoAsync();
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi xử lý giao dịch: " + ex.Message);
                return Page();
            }
        }
    }
}