using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        // Bắt buộc dùng BindProperty để hứng dữ liệu từ Form HTML
        [BindProperty]
        public PHONG PhongDuocChon { get; set; } = new PHONG();

        [BindProperty]
        public DAT_PHONG ThongTinDatPhong { get; set; } = new DAT_PHONG();

        [BindProperty]
        public int SoNgayO { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            try
            {
                // Ưu tiên dùng LINQ truy xuất dữ liệu phòng
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();

                PhongDuocChon = phong;

                // Khởi tạo các giá trị thời gian mặc định
                ThongTinDatPhong.NgayNhan = DateTime.Now;
                ThongTinDatPhong.NgayTra = DateTime.Now.AddDays(1);
                SoNgayO = 1;

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu phòng: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (!ModelState.IsValid)
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong != null) PhongDuocChon = phong;
                return Page();
            }

            try
            {
                // 1. Thêm mới dữ liệu vào bảng DAT_PHONG
                ThongTinDatPhong.MaDatPhong = "DP" + DateTime.Now.ToString("yyyyMMddHHmmss");
                ThongTinDatPhong.NgayDat = DateTime.Now;
                ThongTinDatPhong.TrangThaiDatPhong = "Chờ xác nhận";
                ThongTinDatPhong.KenhDatPhong ??= "Trực tiếp (Walk-in)";

                _context.DAT_PHONG.Add(ThongTinDatPhong);

                // 2. Thêm mới chi tiết vào bảng CHI_TIET_DAT_PHONG
                var chiTiet = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = "CT" + ThongTinDatPhong.MaDatPhong,
                    // ĐÃ SỬA LỖI CS0029: Gán trực tiếp biến int, loại bỏ .ToString()
                    SoNgayO = this.SoNgayO
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTiet);

                // 3. Cập nhật trạng thái trong bảng PHONG (Khóa phòng)
                var phongToUpdate = await _context.PHONG.FindAsync(id);
                if (phongToUpdate != null)
                {
                    phongToUpdate.MaTrangThai = "TT02"; // Mã TT02 quy định là Đã đặt/Đang có khách
                    _context.PHONG.Update(phongToUpdate);
                }

                // Thực thi lưu toàn bộ xuống CSDL
                await _context.SaveChangesAsync();

                // Chuyển hướng người dùng sang trang Xác nhận (Bước tiếp theo của Sprint)
                return RedirectToPage("./XacNhanDatPhong", new { id = ThongTinDatPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu thông tin: " + ex.Message);
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong != null) PhongDuocChon = phong;
                return Page();
            }
        }
    }
}