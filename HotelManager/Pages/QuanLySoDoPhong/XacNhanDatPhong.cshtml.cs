using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class XacNhanDatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public XacNhanDatPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DAT_PHONG ThongTinDatPhong { get; set; } = new DAT_PHONG();
        public PHONG PhongDuocDat { get; set; } = new PHONG();

        public int SoNgayO { get; set; }
        public decimal TongTienPhong { get; set; }
        public decimal SoTienCanCoc { get; set; }

        public async Task<IActionResult> OnGetAsync(string maDatPhong)
        {
            // Kiểm tra bảo mật cơ bản
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");
            if (string.IsNullOrEmpty(maDatPhong)) return RedirectToPage("/Index");

            try
            {
                // Lấy thông tin phiếu đặt phòng
                var datPhong = await _context.DAT_PHONG.AsNoTracking().FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);
                if (datPhong == null) return NotFound();

                ThongTinDatPhong = datPhong;

                // Lấy chi tiết đặt phòng
                var chiTiet = await _context.CHI_TIET_DAT_PHONG.AsNoTracking().FirstOrDefaultAsync(c => c.DAT_PHONGMaDatPhong == maDatPhong);
                if(chiTiet != null)
                {
                    PhongDuocDat = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == chiTiet.PHONGMaPhong) ?? new PHONG();

                    // 1. SỬA LỖI CS0411: Ép kiểu an toàn số ngày ở bằng Convert.ToInt32 (Xử lý mượt mà cả string và int)
                    SoNgayO = Convert.ToInt32(chiTiet.SoNgayO);

                    // 2. SỬA LỖI CS0411: Ép kiểu an toàn đơn giá và tính tổng tiền
                    TongTienPhong = Convert.ToDecimal(chiTiet.DonGia) * SoNgayO;

                    // 3. SỬA LỖI CS0411: Ép kiểu an toàn tiền cọc
                    decimal tienCocDaNhap = Convert.ToDecimal(ThongTinDatPhong.TienCoc);
                    SoTienCanCoc = tienCocDaNhap > 0 ? tienCocDaNhap : (TongTienPhong * 0.3m);
                }

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi truy xuất biên lai: " + ex.Message);
                return Page();
            }
        }
    }
}