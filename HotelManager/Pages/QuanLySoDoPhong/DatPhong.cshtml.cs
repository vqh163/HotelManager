using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class DatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DatPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Chứa thông tin phòng để hiển thị tóm tắt
        public PHONG PhongDuocChon { get; set; } = new PHONG();

        // Chứa thông tin khách hàng lấy tự động từ CSDL
        public KHACH_HANG KhachHangInfo { get; set; } = new KHACH_HANG();

        // Binding dữ liệu form đặt phòng
        [BindProperty]
        public DAT_PHONG ThongTinDatPhong { get; set; } = new DAT_PHONG();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            // Kiểm tra bảo mật: Nếu chưa đăng nhập thì đẩy về trang Đăng nhập kèm ReturnUrl
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/DangNhap", new { ReturnUrl = $"/QuanLySoDoPhong/DatPhong/{id}" });
            }

            if (string.IsNullOrEmpty(id)) return RedirectToPage("/QuanLySoDoPhong/TimPhong");

            try
            {
                // 1. Lấy thông tin phòng
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();
                PhongDuocChon = phong;

                // 2. Tự động lấy thông tin Khách hàng từ tài khoản đang đăng nhập
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null)
                {
                    // Lấy hồ sơ khách hàng dựa trên khóa ngoại (Điều chỉnh tên thuộc tính KHACH_HANGMaKH theo đúng Model thực tế của nhóm)
                    var khachHang = await _context.KHACH_HANG.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);
                    if (khachHang != null)
                    {
                        KhachHangInfo = khachHang;
                    }
                }

                // 3. Khởi tạo giá trị mặc định cho form
                ThongTinDatPhong.NgayNhan = DateTime.Now.Date.AddHours(14); // Mặc định check-in 14:00
                ThongTinDatPhong.NgayTra = DateTime.Now.Date.AddDays(1).AddHours(12); // Mặc định check-out 12:00 hôm sau
                ThongTinDatPhong.SoLuongKhach = 1;

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

            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();

                // Sinh mã đặt phòng tự động (Ví dụ: DP20261004143000)
                ThongTinDatPhong.MaDatPhong = "DP" + DateTime.Now.ToString("yyyyMMddHHmmss");
                ThongTinDatPhong.NgayDat = DateTime.Now;
                ThongTinDatPhong.TrangThaiDatPhong = "Chờ xác nhận"; // Sẽ đổi thành "Thành công" sau khi quét QR thanh toán
                ThongTinDatPhong.KenhDatPhong = "Website Trực tuyến";

                // Tính toán số ngày ở
                int soNgayO = 1;
                if (ThongTinDatPhong.NgayTra.HasValue && ThongTinDatPhong.NgayNhan.HasValue)
                {
                    soNgayO = (ThongTinDatPhong.NgayTra.Value.Date - ThongTinDatPhong.NgayNhan.Value.Date).Days;
                }
                if (soNgayO <= 0) soNgayO = 1;

                // Lưu vào bảng DAT_PHONG
                _context.DAT_PHONG.Add(ThongTinDatPhong);

                // Lưu vào bảng CHI_TIET_DAT_PHONG
                var chiTiet = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = "CT" + ThongTinDatPhong.MaDatPhong,
                    DAT_PHONGMaDatPhong = ThongTinDatPhong.MaDatPhong,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,

                    // Gán trực tiếp biến soNgayO (kiểu int) thay vì gọi .ToString()
                    SoNgayO = soNgayO
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTiet);

                // Cập nhật trạng thái phòng thành Đã đặt (Giữ chỗ)
                phong.MaTrangThai = "TT02";
                _context.PHONG.Update(phong);

                // Lưu toàn bộ Transaction vào CSDL
                await _context.SaveChangesAsync();

                // Thành công -> Chuyển hướng sang trang sinh mã QR thanh toán
                return RedirectToPage("./XacNhanDatPhong", new { maDatPhong = ThongTinDatPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đặt phòng: " + ex.Message);
                return Page();
            }
        }
    }
}