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
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");

            // ===== FIX LỖI VALIDATION ẨN =====
            ModelState.Remove("ThongTinDatPhong.MaDatPhong");
            ModelState.Remove("ThongTinDatPhong.TrangThaiDatPhong");
            ModelState.Remove("ThongTinDatPhong.KenhDatPhong");
            ModelState.Remove("ThongTinDatPhong.KHACH_HANGMaKH");
            ModelState.Remove("ThongTinDatPhong.NgayDat");

            if (!ModelState.IsValid)
            {
                var phongLoadLai = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phongLoadLai != null) PhongDuocChon = phongLoadLai;

                ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra và điền đầy đủ các thông tin bắt buộc!");
                return Page();
            }

            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();

                // [FIX LỖI]: Chặn đặt phòng nếu phòng đang bị Khóa hoặc Bảo trì
                if (phong.MaTrangThai == "TT04")
                {
                    ModelState.AddModelError(string.Empty, "Phòng này hiện đang được bảo trì hoặc bị khóa. Vui lòng chọn phòng khác.");
                    return Page();
                }

                // =================================================================
                // LỚP PHÒNG THỦ 1: KIỂM TRA NGÀY THÁNG HỢP LỆ (Chống F12 bypass)
                // =================================================================
                // [FIX LỖI CS0103]: Khai báo biến today và kiểm tra HasValue để tránh lỗi CS8602
                DateTime today = DateTime.Now.Date;

                if (!ThongTinDatPhong.NgayNhan.HasValue || !ThongTinDatPhong.NgayTra.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Lỗi: Bạn phải chọn Ngày nhận và Ngày trả.");
                    return Page();
                }

                DateTime requestedIn = ThongTinDatPhong.NgayNhan.Value.Date;
                DateTime requestedOut = ThongTinDatPhong.NgayTra.Value.Date;

                if (requestedIn < today)
                {
                    ModelState.AddModelError(string.Empty, "Lỗi: Ngày nhận phòng không được chọn trong quá khứ.");
                    return Page();
                }

                // =================================================================
                // LỚP PHÒNG THỦ 2: KIỂM TRA OVERLAP DATE (Chống Double-Booking)
                // =================================================================
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
                    ModelState.AddModelError(string.Empty, "Phòng này vừa được đặt trong khoảng thời gian trên. Vui lòng chọn ngày khác.");
                    return Page();
                }

                // =================================================================
                // XỬ LÝ ĐỒNG BỘ HỒ SƠ KHÁCH HÀNG O2O (UC_03 & UC_05)
                // =================================================================
                // ... (GIỮ NGUYÊN TOÀN BỘ ĐOẠN CODE ĐỒNG BỘ O2O VÀ LƯU DATABASE TỪ ĐÂY TRỞ XUỐNG) ...

                // =================================================================
                // XỬ LÝ ĐỒNG BỘ HỒ SƠ KHÁCH HÀNG O2O (UC_03 & UC_05)
                // =================================================================
                string maKhachHangChot = string.Empty;
                var tenDangNhap = User.Identity.Name;
                var taiKhoanDangNhap = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoanDangNhap != null)
                {
                    // Lễ tân / Quản lý đặt hộ khách tại quầy
                    if (taiKhoanDangNhap.VaiTro == "Lễ tân" || taiKhoanDangNhap.VaiTro == "Quản lý")
                    {
                        // Kiểm tra xem SDT khách đã có trong CSDL chưa
                        var khachHangCu = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.SDT == SDTKhach);
                        if (khachHangCu != null)
                        {
                            // Khách quen -> Tái sử dụng MaKH để đồng bộ lịch sử
                            maKhachHangChot = khachHangCu.MaKH;
                            khachHangCu.HoTen = HoTenKhach;
                            khachHangCu.Email = EmailKhach;
                            _context.KHACH_HANG.Update(khachHangCu);
                        }
                        else
                        {
                            // Khách mới -> Tạo hồ sơ mới
                            maKhachHangChot = "KH" + DateTime.Now.ToString("yyyyMMddHHmmss");
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
                    else // Khách hàng tự đặt trên Web
                    {
                        if (!string.IsNullOrEmpty(taiKhoanDangNhap.KHACH_HANGMaKH))
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
                    }
                }

                // Gắn mã khách hàng vừa chốt vào phiếu đặt phòng
                ThongTinDatPhong.KHACH_HANGMaKH = maKhachHangChot;
                ThongTinDatPhong.MaDatPhong = "DP" + DateTime.Now.ToString("yyyyMMddHHmmss");
                ThongTinDatPhong.NgayDat = DateTime.Now;
                ThongTinDatPhong.TrangThaiDatPhong = "Chờ xác nhận";
                
                // Lưu vết Kênh đặt phòng để phục vụ báo cáo doanh thu UC_12
                ThongTinDatPhong.KenhDatPhong = (taiKhoanDangNhap?.VaiTro == "Lễ tân" || taiKhoanDangNhap?.VaiTro == "Quản lý") ? "Trực tiếp tại quầy" : "Website Trực tuyến";

                // Tính toán số ngày ở
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

                phong.MaTrangThai = "TT02"; // Chuyển phòng sang trạng thái Đã đặt
                _context.PHONG.Update(phong);

                // Lưu toàn bộ Transaction vào CSDL
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