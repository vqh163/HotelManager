using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models; // Chứa các entity KHACH_HANG, DAT_PHONG, PHONG...[cite: 12]

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class XacNhanDatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public XacNhanDatPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dữ liệu phòng để hiển thị ra cột trái của giao diện
        public PHONG PhongDuocChon { get; set; } = default!;

        // Thuộc tính nhận dữ liệu ngày nhận, ngày trả từ trang Tìm Phòng truyền sang
        [BindProperty(SupportsGet = true)]
        public DateTime NgayNhan { get; set; } = new DateTime(2026, 10, 2); // Mặc định theo UI mẫu
        [BindProperty(SupportsGet = true)]
        public DateTime NgayTra { get; set; } = new DateTime(2026, 10, 4);

        public int SoDem => (NgayTra - NgayNhan).Days > 0 ? (NgayTra - NgayNhan).Days : 1;

        // Đối tượng chứa dữ liệu form Khách hàng nhập vào
        [BindProperty]
        public ThongTinDatPhong Input { get; set; } = new ThongTinDatPhong();

        public class ThongTinDatPhong
        {
            public string HoTen { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string SoDienThoai { get; set; } = string.Empty;
            public string? YeuCauDacBiet { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string maPhong)
        {
            try
            {
                // Dùng LINQ lấy thông tin phòng được chọn
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong == null)
                {
                    return RedirectToPage("/QuanLySoDoPhong/TimPhong");
                }

                PhongDuocChon = phong;
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống: {ex.Message}");
                return RedirectToPage("/QuanLySoDoPhong/TimPhong");
            }
        }

        public async Task<IActionResult> OnPostAsync(string maPhong)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Mở một Transaction vì chúng ta insert vào nhiều bảng cùng lúc (KHACH_HANG, DAT_PHONG)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy giá phòng
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong == null) throw new Exception("Không tìm thấy phòng.");

                // 2. Lưu thông tin khách hàng (Bỏ qua bước check tồn tại cho code gọn, thực tế cần check theo SĐT/CCCD)
                var khachHang = new KHACH_HANG
                {
                    MaKH = "KH" + DateTime.Now.Ticks.ToString().Substring(10), // Tạo mã ngẫu nhiên
                    HoTen = Input.HoTen,
                    Email = Input.Email,
                    SDT = Input.SoDienThoai,
                    GhiChuDacBiet = Input.YeuCauDacBiet
                };
                _context.KHACH_HANG.Add(khachHang);

                // 3. Tạo Đơn Đặt Phòng
                var datPhong = new DAT_PHONG
                {
                    MaDatPhong = "BKG" + DateTime.Now.ToString("yyMMddHHmm"),
                    NgayDat = DateTime.Now,
                    NgayNhan = NgayNhan,
                    NgayTra = NgayTra,
                    TrangThaiDatPhong = "CHUA_THANH_TOAN",
                    KenhDatPhong = "WEBSITE",
                    GhiChu = Input.YeuCauDacBiet
                };
                _context.DAT_PHONG.Add(datPhong);

                // 4. Lưu CSDL và Commit Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Chuyển hướng đến trang thông báo thành công hoặc trang thanh toán
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"Lỗi khi đặt phòng: {ex.Message}");
                // Cần load lại thông tin phòng nếu form lỗi
                PhongDuocChon = await _context.PHONG.FirstAsync(p => p.MaPhong == maPhong);
                return Page();
            }
        }
    }
}