using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyHoSo
{
    public class HoSoKhachHangModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HoSoKhachHangModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public KHACH_HANG KhachHangInfo { get; set; } = new KHACH_HANG();

        [TempData]
        public string ThongBao { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");

            try
            {
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                {
                    var kh = await _context.KHACH_HANG.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);
                    if (kh != null)
                    {
                        // THUẬT TOÁN XÉT HẠNG THÀNH VIÊN TỰ ĐỘNG DỰA TRÊN ĐIỂM THƯỞNG
                        int diem = kh.DiemThuong.GetValueOrDefault(0);
                        if (diem < 100) kh.HangThanhVien = "Thành viên mới";
                        else if (diem < 500) kh.HangThanhVien = "Bạc";
                        else if (diem < 1000) kh.HangThanhVien = "Vàng";
                        else kh.HangThanhVien = "Kim cương";

                        KhachHangInfo = kh;
                    }
                }
                return Page();
            }
            catch (Exception ex)
            {
                ThongBao = "Lỗi hệ thống khi tải hồ sơ: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Kiểm tra Validate (bao gồm cả regex CCCD từ Model)
            if (!ModelState.IsValid)
            {
                ThongBao = "❌ Vui lòng kiểm tra lại các thông tin nhập chưa đúng!";
                return Page();
            }

            // Xóa đoạn kiểm tra thủ công CCCD vì bây giờ Frontend và ModelState đã lo việc này
            try
            {
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                {
                    var khUpdate = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);

                    if (khUpdate != null)
                    {
                        khUpdate.HoTen = KhachHangInfo.HoTen;
                        khUpdate.SDT = KhachHangInfo.SDT;
                        khUpdate.Email = KhachHangInfo.Email;
                        khUpdate.SoCCCD = KhachHangInfo.SoCCCD;
                        khUpdate.GioiTinh = KhachHangInfo.GioiTinh;
                        khUpdate.NgaySinh = KhachHangInfo.NgaySinh;

                        // Tự động xét hạng
                        int diem = khUpdate.DiemThuong.GetValueOrDefault(0);
                        if (diem < 100) khUpdate.HangThanhVien = "Thành viên mới";
                        else if (diem < 500) khUpdate.HangThanhVien = "Bạc";
                        else if (diem < 1000) khUpdate.HangThanhVien = "Vàng";
                        else khUpdate.HangThanhVien = "Kim cương";

                        _context.KHACH_HANG.Update(khUpdate);
                        await _context.SaveChangesAsync();

                        ThongBao = "✅ Cập nhật hồ sơ thành công!";
                    }
                }

                // Mẹo nhỏ: RedirectToPage thay vì return Page() để tránh lỗi resubmit khi ấn F5
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ThongBao = "❌ Có lỗi xảy ra trong quá trình lưu: " + ex.Message;
                return Page();
            }
        }
    }
}