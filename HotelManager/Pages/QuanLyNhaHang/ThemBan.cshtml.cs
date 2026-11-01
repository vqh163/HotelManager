using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class ThemBanViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã bàn.")]
        public string MaBan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên/số bàn.")]
        public string TenBan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập sức chứa.")]
        public int? SucChua { get; set; }

        public string TrangThai { get; set; } = "Trống";

        public string? TenKhachDat { get; set; }
        public string? SoPhongDat { get; set; }
    }

    public class ThemBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ThemBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ThemBanViewModel BanInfo { get; set; } = new ThemBanViewModel();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                var ban = new BAN_NHA_HANG
                {
                    MaBan = BanInfo.MaBan?.Trim(), // FIX 3: Xóa khoảng trắng thừa để tránh lỗi map khóa ngoại
                    TenBan = BanInfo.TenBan?.Trim(),
                    SucChua = BanInfo.SucChua,
                    TrangThai = BanInfo.TrangThai
                };

                // Gán dữ liệu khách đặt an toàn
                if (ban.GetType().GetProperty("TenKhachDat") != null)
                {
                    ban.GetType().GetProperty("TenKhachDat")?.SetValue(ban, BanInfo.TrangThai == "Đã đặt trước" ? BanInfo.TenKhachDat : null);
                }

                // BỔ SUNG: Bạn đang quên lưu SoPhongDat lấy từ form
                if (ban.GetType().GetProperty("SoPhongDat") != null)
                {
                    ban.GetType().GetProperty("SoPhongDat")?.SetValue(ban, BanInfo.TrangThai == "Đã đặt trước" ? BanInfo.SoPhongDat : null);
                }

                _context.BAN_NHA_HANG.Add(ban);

                // Sinh Hóa đơn tự động nếu Đang có khách
                if (BanInfo.TrangThai == "Đang có khách")
                {
                    string uniqueInvoiceId = "HD" + DateTime.Now.ToString("yyMMdd") + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

                    var hoaDonMoi = new HOA_DON_NHA_HANG
                    {
                        MaHoaDonNhaHang = uniqueInvoiceId,
                        ThoiGianTao = DateTime.Now,
                        TongTienMonAn = 0,
                        GiamGia = 0,
                        ThanhTien = 0,
                        HinhThucThanhToan = "Đang phục vụ"
                    };

                    // FIX 1: Gán trạng thái cho hóa đơn (Rất quan trọng để Sơ đồ bàn nhận diện hóa đơn đang Active)
                    if (hoaDonMoi.GetType().GetProperty("TrangThai") != null)
                    {
                        // Tùy theo thiết kế DB của bạn, có thể là chuỗi "Chưa thanh toán", "Đang phục vụ" hoặc số 0, 1
                        hoaDonMoi.GetType().GetProperty("TrangThai")?.SetValue(hoaDonMoi, "Chưa thanh toán");
                    }

                    // FIX 2: Gán ngược Mã Hóa Đơn cho Bàn (Nếu bảng Bàn có lưu cột này để hiển thị UI nhanh)
                    if (ban.GetType().GetProperty("MaHoaDonHienTai") != null)
                    {
                        ban.GetType().GetProperty("MaHoaDonHienTai")?.SetValue(ban, uniqueInvoiceId);
                    }

                    var dbProperties = hoaDonMoi.GetType().GetProperties();
                    foreach (var prop in dbProperties)
                    {
                        if (prop.Name == "MaBan" || prop.Name.EndsWith("MaBan"))
                        {
                            prop.SetValue(hoaDonMoi, ban.MaBan); // Sử dụng ban.MaBan đã được Trim() ở trên
                            break;
                        }
                    }

                    _context.HOA_DON_NHA_HANG.Add(hoaDonMoi);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm bàn mới thành công!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi thêm bàn. Có thể Mã bàn đã tồn tại: " + ex.Message);
                return Page();
            }
        }
    }
}