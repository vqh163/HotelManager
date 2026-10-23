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
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");

            // ===== FIX LỖI VALIDATION ẨN =====
            // Gỡ bỏ kiểm tra lỗi cho các thuộc tính không có trên Form HTML (do Server tự sinh)
            ModelState.Remove("ThongTinDatPhong.MaDatPhong");
            ModelState.Remove("ThongTinDatPhong.TrangThaiDatPhong");
            ModelState.Remove("ThongTinDatPhong.KenhDatPhong");
            ModelState.Remove("ThongTinDatPhong.KHACH_HANGMaKH");
            ModelState.Remove("ThongTinDatPhong.NgayDat");

            // Lúc này ModelState mới đánh giá chính xác các trường khách hàng thực sự nhập
            if (!ModelState.IsValid)
            {
                // Load lại thông tin phòng để giao diện không bị vỡ khi báo lỗi
                var phongLoadLai = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phongLoadLai != null) PhongDuocChon = phongLoadLai;

                ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra và điền đầy đủ các thông tin bắt buộc!");
                return Page();
            }

            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();

                DateTime requestedIn = ThongTinDatPhong.NgayNhan.Value.Date;
                DateTime requestedOut = ThongTinDatPhong.NgayTra.Value.Date;

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
                    ModelState.AddModelError(string.Empty, "Phòng này vừa được đặt. Vui lòng chọn ngày khác.");
                    return Page();
                }

                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                {
                    ThongTinDatPhong.KHACH_HANGMaKH = taiKhoan.KHACH_HANGMaKH;

                    // TỰ ĐỘNG CẬP NHẬT THÔNG TIN CÁ NHÂN VÀO CSDL NẾU KHÁCH NHẬP MỚI
                    var khachHang = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);
                    if (khachHang != null)
                    {
                        khachHang.HoTen = HoTenKhach;
                        khachHang.SDT = SDTKhach;
                        khachHang.Email = EmailKhach;
                        _context.KHACH_HANG.Update(khachHang);
                    }
                }

                ThongTinDatPhong.MaDatPhong = "DP" + DateTime.Now.ToString("yyyyMMddHHmmss");
                ThongTinDatPhong.NgayDat = DateTime.Now;
                ThongTinDatPhong.TrangThaiDatPhong = "Chờ xác nhận";
                ThongTinDatPhong.KenhDatPhong = "Website Trực tuyến";

                int soNgayO = 1;
                if (ThongTinDatPhong.NgayTra.HasValue && ThongTinDatPhong.NgayNhan.HasValue)
                {
                    soNgayO = (ThongTinDatPhong.NgayTra.Value.Date - ThongTinDatPhong.NgayNhan.Value.Date).Days;
                }
                if (soNgayO <= 0) soNgayO = 1;

                _context.DAT_PHONG.Add(ThongTinDatPhong);

                var chiTiet = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = "CT" + ThongTinDatPhong.MaDatPhong,
                    DAT_PHONGMaDatPhong = ThongTinDatPhong.MaDatPhong,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,
                    SoNgayO = soNgayO
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTiet);

                phong.MaTrangThai = "TT02";
                _context.PHONG.Update(phong);

                await _context.SaveChangesAsync();
                return RedirectToPage("./XacNhanDatPhong", new { maDatPhong = ThongTinDatPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi: " + ex.Message);
                return Page();
            }
        }
    }
}