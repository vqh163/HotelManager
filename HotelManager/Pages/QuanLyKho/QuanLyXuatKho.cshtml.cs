using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    public class QuanLyXuatKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyXuatKhoModel> _logger;

        public QuanLyXuatKhoModel(ApplicationDbContext context, ILogger<QuanLyXuatKhoModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();
        public IList<QuanLyNhapKhoModel.PhieuKhoViewModel> LichSuXuatKho { get; set; } = new List<QuanLyNhapKhoModel.PhieuKhoViewModel>();

        [BindProperty]
        public XuatKhoInputModel Input { get; set; } = new XuatKhoInputModel();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class XuatKhoInputModel
        {
            [Required(ErrorMessage = "Vui lòng chọn vật tư cần xuất.")]
            public string MaVatTu { get; set; } = string.Empty;

            [Range(1, int.MaxValue, ErrorMessage = "Số lượng xuất phải lớn hơn 0.")]
            public int SoLuongXuat { get; set; } = 1;

            [Required(ErrorMessage = "Vui lòng chọn bộ phận tiếp nhận.")]
            public string BoPhanNhan { get; set; } = "Bộ phận Buồng phòng";

            [Required(ErrorMessage = "Vui lòng nhập lý do xuất kho.")]
            public string LyDoXuat { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                DanhSachVatTu = await _context.KHO_VAT_TU
                    .AsNoTracking()
                    .OrderBy(v => v.TenVatTu)
                    .ToListAsync();

                // Truy vấn lịch sử phiếu XUẤT (bao gồm cả xuất cho sửa chữa sự cố) trực tiếp từ SQL Server
                LichSuXuatKho = await _context.PHIEU_NHAP_XUAT_KHO
                    .AsNoTracking()
                    .Where(p => p.LoaiGiaoDich == "XUAT")
                    .OrderByDescending(p => p.ThoiGian)
                    .Take(100)
                    .Select(p => new QuanLyNhapKhoModel.PhieuKhoViewModel
                    {
                        MaPhieu = p.MaPhieu,
                        LoaiGiaoDich = p.LoaiGiaoDich,
                        MaVatTu = p.KHO_VAT_TUMaVatTu,
                        TenVatTu = p.TenVatTu,
                        LoaiKho = p.LoaiKho,
                        DonViTinh = p.DonViTinh,
                        SoLuong = p.SoLuong,
                        DonGia = p.DonGia,
                        DoiTacHoacBoPhan = p.DoiTacHoacBoPhan,
                        GhiChu = p.GhiChu ?? string.Empty,
                        ThoiGian = p.ThoiGian
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải dữ liệu xuất kho.");
                ThongBaoLoi = "Đã xảy ra lỗi khi tải dữ liệu xuất kho.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                ThongBaoLoi = "Vui lòng kiểm tra lại thông tin phiếu xuất kho.";
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vatTuDb = await _context.KHO_VAT_TU.AsNoTracking().FirstOrDefaultAsync(v => v.MaVatTu == Input.MaVatTu);
                if (vatTuDb == null)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = "Không tìm thấy vật tư cần xuất trong cơ sở dữ liệu!";
                    return Page();
                }

                // Trừ kho nguyên tử (Atomic Update) với điều kiện tồn kho >= số lượng xuất để chống âm kho khi đa luồng
                int affected = await _context.KHO_VAT_TU
                    .Where(v => v.MaVatTu == Input.MaVatTu && (v.SoLuongTon ?? 0) >= Input.SoLuongXuat)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.SoLuongTon, v => (v.SoLuongTon ?? 0) - Input.SoLuongXuat));

                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    await LoadDataAsync();
                    ThongBaoLoi = $"Không đủ hàng để xuất! Vật tư '{vatTuDb.TenVatTu}' hiện chỉ còn tồn {vatTuDb.SoLuongTon ?? 0} {vatTuDb.DonViTinh}.";
                    return Page();
                }

                int tonConLai = (vatTuDb.SoLuongTon ?? 0) - Input.SoLuongXuat;
                string maPhieu = BaoMatHelper.SinhMaNgauNhien("PX");

                _context.PHIEU_NHAP_XUAT_KHO.Add(new PHIEU_NHAP_XUAT_KHO
                {
                    MaPhieu = maPhieu,
                    LoaiGiaoDich = "XUAT",
                    KHO_VAT_TUMaVatTu = vatTuDb.MaVatTu,
                    TenVatTu = vatTuDb.TenVatTu ?? "Vật tư",
                    LoaiKho = vatTuDb.LoaiVatTu ?? "Kho chung",
                    DonViTinh = vatTuDb.DonViTinh ?? "Cái",
                    SoLuong = Input.SoLuongXuat,
                    DonGia = vatTuDb.DonGiaNhap ?? 0,
                    DoiTacHoacBoPhan = Input.BoPhanNhan.Trim(),
                    GhiChu = Input.LyDoXuat.Trim(),
                    NguoiThucHien = User.Identity?.Name,
                    ThoiGian = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                int tonToiThieu = vatTuDb.SoLuongTonToiThieu ?? 0;
                string canhBao = tonConLai <= tonToiThieu
                    ? $" (CẢNH BÁO: Tồn kho còn {tonConLai}, đã chạm mức tối thiểu {tonToiThieu} - Cần lên đề xuất mua hàng!)"
                    : $" (Tồn còn lại: {tonConLai}).";

                ThongBao = $"Lập phiếu xuất [{maPhieu}] thành công! Đã xuất {Input.SoLuongXuat} {vatTuDb.DonViTinh} '{vatTuDb.TenVatTu}' cho {Input.BoPhanNhan}{canhBao}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hệ thống khi xuất kho.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi xuất kho.";
                await LoadDataAsync();
                return Page();
            }
        }
    }
}
