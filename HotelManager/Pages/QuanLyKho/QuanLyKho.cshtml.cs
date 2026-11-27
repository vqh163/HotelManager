using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    public class QuanLyKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyKhoModel> _logger;

        public QuanLyKhoModel(ApplicationDbContext context, ILogger<QuanLyKhoModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();
        public int TongSoMatHang { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public string LoaiVatTuFilter { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        [BindProperty]
        public KHO_VAT_TU InputData { get; set; } = new KHO_VAT_TU();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            try
            {
                TongSoMatHang = await _context.KHO_VAT_TU.AsNoTracking().CountAsync();
                var query = _context.KHO_VAT_TU.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(LoaiVatTuFilter) && LoaiVatTuFilter != "ALL")
                {
                    query = query.Where(v => v.LoaiVatTu == LoaiVatTuFilter);
                }

                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    string keyword = TuKhoa.Trim();
                    query = query.Where(v =>
                        (v.TenVatTu != null && v.TenVatTu.Contains(keyword)) ||
                        v.MaVatTu.Contains(keyword));
                }

                DanhSachVatTu = await query
                    .OrderBy(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0) ? 0 : 1)
                    .ThenBy(v => v.TenVatTu)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh mục kho.");
                ThongBaoLoi = "Đã xảy ra lỗi khi tải danh mục kho vật tư.";
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InputData.TenVatTu))
                {
                    ThongBaoLoi = "Tên vật tư không được để trống.";
                    return RedirectToPage(new { LoaiVatTuFilter, TuKhoa });
                }

                int tonBanDau = Math.Max(0, InputData.SoLuongTon ?? 0);
                int tonToiThieu = Math.Max(0, InputData.SoLuongTonToiThieu ?? 0);
                decimal donGia = Math.Max(0, InputData.DonGiaNhap ?? 0);

                var vatTuMoi = new KHO_VAT_TU
                {
                    MaVatTu = BaoMatHelper.SinhMaNgauNhien("VT"),
                    TenVatTu = InputData.TenVatTu.Trim(),
                    LoaiVatTu = InputData.LoaiVatTu,
                    DonViTinh = InputData.DonViTinh?.Trim(),
                    SoLuongTon = tonBanDau,
                    SoLuongTonToiThieu = tonToiThieu,
                    DonGiaNhap = donGia
                };

                _context.KHO_VAT_TU.Add(vatTuMoi);
                await _context.SaveChangesAsync();

                ThongBao = $"Đã thêm mới hàng hóa '{vatTuMoi.TenVatTu}' vào {vatTuMoi.LoaiVatTu}!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm mới vật tư.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi thêm mới vật tư.";
            }

            return RedirectToPage(new { LoaiVatTuFilter, TuKhoa });
        }

        // Cập nhật thông tin danh mục vật tư (KHÔNG cho sửa trực tiếp SoLuongTon để tránh thất thoát kho)
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            try
            {
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == InputData.MaVatTu);
                if (vatTuDb != null)
                {
                    vatTuDb.TenVatTu = InputData.TenVatTu?.Trim();
                    vatTuDb.LoaiVatTu = InputData.LoaiVatTu;
                    vatTuDb.DonViTinh = InputData.DonViTinh?.Trim();
                    vatTuDb.DonGiaNhap = Math.Max(0, InputData.DonGiaNhap ?? 0);
                    vatTuDb.SoLuongTonToiThieu = Math.Max(0, InputData.SoLuongTonToiThieu ?? 0);

                    await _context.SaveChangesAsync();
                    ThongBao = $"Đã cập nhật thông tin danh mục '{vatTuDb.TenVatTu}' thành công! (Số lượng tồn kho chỉ thay đổi qua Nhập/Xuất/Kiểm kê).";
                }
                else
                {
                    ThongBaoLoi = "Không tìm thấy mã vật tư cần cập nhật trong cơ sở dữ liệu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật vật tư {MaVT}", InputData.MaVatTu);
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi cập nhật vật tư.";
            }

            return RedirectToPage(new { LoaiVatTuFilter, TuKhoa });
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                // Chặn xóa vật tư đã phát sinh giao dịch kho, sự cố hoặc dịch vụ
                bool daSuDung = await _context.PHIEU_NHAP_XUAT_KHO.AsNoTracking().AnyAsync(p => p.KHO_VAT_TUMaVatTu == id)
                             || await _context.PHIEU_BAO_CAO_SU_CO.AsNoTracking().AnyAsync(s => s.KHO_VAT_TUMaVatTu == id)
                             || await _context.CHI_TIET_DICH_VU.AsNoTracking().AnyAsync(c => c.KHO_VAT_TUMaVatTu == id);
                if (daSuDung)
                {
                    ThongBaoLoi = "Không thể xóa vật tư này vì đã phát sinh phiếu nhập/xuất kho hoặc bảo trì sự cố liên quan!";
                    return RedirectToPage(new { LoaiVatTuFilter, TuKhoa });
                }

                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == id);
                if (vatTuDb != null)
                {
                    string tenTam = vatTuDb.TenVatTu ?? id;
                    _context.KHO_VAT_TU.Remove(vatTuDb);
                    await _context.SaveChangesAsync();
                    ThongBao = $"Đã xóa vật tư '{tenTam}' khỏi danh mục kho.";
                }
                else
                {
                    ThongBaoLoi = "Không tìm thấy vật tư cần xóa.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa vật tư {Id}", id);
                ThongBaoLoi = "Không thể xóa vật tư do lỗi hệ thống.";
            }

            return RedirectToPage(new { LoaiVatTuFilter, TuKhoa });
        }
    }
}
