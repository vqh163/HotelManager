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
    public class QuanLyNhapKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyNhapKhoModel> _logger;

        public QuanLyNhapKhoModel(ApplicationDbContext context, ILogger<QuanLyNhapKhoModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();
        public IList<PhieuKhoViewModel> LichSuNhapKho { get; set; } = new List<PhieuKhoViewModel>();

        [BindProperty]
        public NhapKhoInputModel Input { get; set; } = new NhapKhoInputModel();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class NhapKhoInputModel
        {
            [Required(ErrorMessage = "Vui lòng chọn vật tư cần nhập.")]
            public string MaVatTu { get; set; } = string.Empty;

            [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0.")]
            public int SoLuongNhap { get; set; } = 10;

            [Range(0, double.MaxValue, ErrorMessage = "Đơn giá nhập không hợp lệ.")]
            public decimal DonGiaNhapMoi { get; set; } = 0;

            [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
            public string NhaCungCap { get; set; } = string.Empty;

            public string? GhiChu { get; set; }
        }

        public class PhieuKhoViewModel
        {
            public string MaPhieu { get; set; } = string.Empty;
            public string LoaiGiaoDich { get; set; } = string.Empty;
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiKho { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien => SoLuong * DonGia;
            public string DoiTacHoacBoPhan { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
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

                // Truy vấn lịch sử phiếu NHẬP trực tiếp từ bảng PHIEU_NHAP_XUAT_KHO bằng LINQ
                LichSuNhapKho = await _context.PHIEU_NHAP_XUAT_KHO
                    .AsNoTracking()
                    .Where(p => p.LoaiGiaoDich == "NHAP")
                    .OrderByDescending(p => p.ThoiGian)
                    .Take(100)
                    .Select(p => new PhieuKhoViewModel
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
                _logger.LogError(ex, "Lỗi tải dữ liệu nhập kho.");
                ThongBaoLoi = "Đã xảy ra lỗi khi tải dữ liệu nhập kho.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                ThongBaoLoi = "Vui lòng kiểm tra lại thông tin phiếu nhập.";
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == Input.MaVatTu);
                if (vatTuDb == null)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = "Không tìm thấy vật tư trong cơ sở dữ liệu!";
                    return Page();
                }

                // 1. Cộng dồn tồn kho và cập nhật đơn giá nhập mới nhất
                vatTuDb.SoLuongTon = (vatTuDb.SoLuongTon ?? 0) + Input.SoLuongNhap;
                vatTuDb.DonGiaNhap = Input.DonGiaNhapMoi;

                // 2. Lưu phiếu nhập kho vào SQL Server trong cùng Transaction (đảm bảo ACID)
                string maPhieu = BaoMatHelper.SinhMaNgauNhien("PN");
                _context.PHIEU_NHAP_XUAT_KHO.Add(new PHIEU_NHAP_XUAT_KHO
                {
                    MaPhieu = maPhieu,
                    LoaiGiaoDich = "NHAP",
                    KHO_VAT_TUMaVatTu = vatTuDb.MaVatTu,
                    TenVatTu = vatTuDb.TenVatTu ?? "Vật tư",
                    LoaiKho = vatTuDb.LoaiVatTu ?? "Kho chung",
                    DonViTinh = vatTuDb.DonViTinh ?? "Cái",
                    SoLuong = Input.SoLuongNhap,
                    DonGia = Input.DonGiaNhapMoi,
                    DoiTacHoacBoPhan = Input.NhaCungCap.Trim(),
                    GhiChu = Input.GhiChu?.Trim(),
                    NguoiThucHien = User.Identity?.Name,
                    ThoiGian = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Lập phiếu nhập [{maPhieu}] thành công! Đã cộng thêm {Input.SoLuongNhap} {vatTuDb.DonViTinh} '{vatTuDb.TenVatTu}' vào kho (Tồn mới: {vatTuDb.SoLuongTon}).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi lập phiếu nhập kho.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi nhập kho.";
                await LoadDataAsync();
                return Page();
            }
        }
    }
}
