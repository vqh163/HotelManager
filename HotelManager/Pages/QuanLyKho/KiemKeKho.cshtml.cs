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
    public class KiemKeKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KiemKeKhoModel> _logger;

        public KiemKeKhoModel(ApplicationDbContext context, ILogger<KiemKeKhoModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string LoaiVatTuFilter { get; set; } = "ALL";

        [BindProperty]
        public string KyKiemKe { get; set; } = "Kiểm kê định kỳ Hàng Tuần";

        [BindProperty]
        public string NguoiKiemKe { get; set; } = string.Empty;

        [BindProperty]
        public string? GhiChuChung { get; set; }

        [BindProperty]
        public List<KiemKeItemInput> ItemsKiemKe { get; set; } = new List<KiemKeItemInput>();

        public List<BienBanKiemKeViewModel> LichSuKiemKe { get; set; } = new List<BienBanKiemKeViewModel>();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class KiemKeItemInput
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public decimal DonGiaNhap { get; set; }
            public int TonHeThong { get; set; }
            public int TonThucTe { get; set; }
            public string? LyDoChenhLech { get; set; }
        }

        public class BienBanKiemKeViewModel
        {
            public string MaBienBan { get; set; } = string.Empty;
            public string KyKiemKe { get; set; } = string.Empty;
            public string PhamViKho { get; set; } = string.Empty;
            public string NguoiKiemKe { get; set; } = string.Empty;
            public int TongMatHang { get; set; }
            public int SoMatHangLech { get; set; }
            public decimal GiaTriChenhLech { get; set; }
            public string GhiChu { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
        }

        public async Task OnGetAsync()
        {
            try
            {
                NguoiKiemKe = User.Identity?.Name ?? "Thủ kho";

                var query = _context.KHO_VAT_TU.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(LoaiVatTuFilter) && LoaiVatTuFilter != "ALL")
                {
                    query = query.Where(v => v.LoaiVatTu == LoaiVatTuFilter);
                }

                var danhSachDb = await query.OrderBy(v => v.LoaiVatTu).ThenBy(v => v.TenVatTu).ToListAsync();

                ItemsKiemKe = danhSachDb.Select(v => new KiemKeItemInput
                {
                    MaVatTu = v.MaVatTu,
                    TenVatTu = v.TenVatTu ?? "Chưa đặt tên",
                    LoaiVatTu = v.LoaiVatTu ?? "Chưa phân loại",
                    DonViTinh = v.DonViTinh ?? "Cái",
                    DonGiaNhap = v.DonGiaNhap ?? 0,
                    TonHeThong = v.SoLuongTon ?? 0,
                    TonThucTe = v.SoLuongTon ?? 0,
                    LyDoChenhLech = string.Empty
                }).ToList();

                // Đọc lịch sử biên bản kiểm kê trực tiếp từ SQL Server bằng LINQ
                LichSuKiemKe = await _context.BIEN_BAN_KIEM_KE
                    .AsNoTracking()
                    .OrderByDescending(b => b.ThoiGian)
                    .Take(50)
                    .Select(b => new BienBanKiemKeViewModel
                    {
                        MaBienBan = b.MaBienBan,
                        KyKiemKe = b.KyKiemKe,
                        PhamViKho = b.PhamViKho,
                        NguoiKiemKe = b.NguoiKiemKe,
                        TongMatHang = b.TongMatHang,
                        SoMatHangLech = b.SoMatHangLech,
                        GiaTriChenhLech = b.GiaTriChenhLech,
                        GhiChu = b.GhiChu ?? string.Empty,
                        ThoiGian = b.ThoiGian
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách kiểm kê kho.");
                ThongBaoLoi = "Đã xảy ra lỗi khi tải danh sách kiểm kê kho.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ItemsKiemKe == null || !ItemsKiemKe.Any())
            {
                ThongBaoLoi = "Không có dữ liệu vật tư nào để chốt kiểm kê.";
                return RedirectToPage(new { LoaiVatTuFilter });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Luôn lấy tên người kiểm kê từ tài khoản đã đăng nhập (chống giả mạo)
                string nguoiThucHien = User.Identity?.Name ?? "Thủ kho";
                string maBienBan = BaoMatHelper.SinhMaNgauNhien("KK");
                int soMatHangLech = 0;
                decimal tongGiaTriLech = 0;
                DateTime now = DateTime.Now;

                var maVatTuList = ItemsKiemKe.Select(x => x.MaVatTu).ToList();
                var vatTuDbList = await _context.KHO_VAT_TU
                    .Where(v => maVatTuList.Contains(v.MaVatTu))
                    .ToListAsync();

                foreach (var item in ItemsKiemKe)
                {
                    var vtDb = vatTuDbList.FirstOrDefault(v => v.MaVatTu == item.MaVatTu);
                    if (vtDb == null) continue;

                    int tonCu = vtDb.SoLuongTon ?? 0;
                    int tonMoi = item.TonThucTe < 0 ? 0 : item.TonThucTe;
                    int chenhLech = tonMoi - tonCu;

                    if (chenhLech != 0)
                    {
                        soMatHangLech++;
                        decimal donGia = vtDb.DonGiaNhap ?? 0;
                        tongGiaTriLech += chenhLech * donGia;
                        vtDb.SoLuongTon = tonMoi;

                        string loaiGD = chenhLech > 0 ? "NHAP" : "XUAT";
                        string dienGiai = $"[Cân bằng {KyKiemKe} - {maBienBan}] " +
                            (string.IsNullOrWhiteSpace(item.LyDoChenhLech) ? "Điều chỉnh chênh lệch kiểm kê" : item.LyDoChenhLech.Trim());

                        _context.PHIEU_NHAP_XUAT_KHO.Add(new PHIEU_NHAP_XUAT_KHO
                        {
                            MaPhieu = maBienBan,
                            LoaiGiaoDich = loaiGD,
                            KHO_VAT_TUMaVatTu = vtDb.MaVatTu,
                            TenVatTu = vtDb.TenVatTu ?? "Vật tư",
                            LoaiKho = vtDb.LoaiVatTu ?? "Kho chung",
                            DonViTinh = vtDb.DonViTinh ?? "Cái",
                            SoLuong = Math.Abs(chenhLech),
                            DonGia = donGia,
                            DoiTacHoacBoPhan = $"Kiểm kê ({nguoiThucHien})",
                            GhiChu = dienGiai,
                            NguoiThucHien = nguoiThucHien,
                            MaThamChieu = maBienBan,
                            ThoiGian = now
                        });
                    }
                }

                string phamVi = string.IsNullOrEmpty(LoaiVatTuFilter) || LoaiVatTuFilter == "ALL" ? "Tất cả kho" : LoaiVatTuFilter;
                _context.BIEN_BAN_KIEM_KE.Add(new BIEN_BAN_KIEM_KE
                {
                    MaBienBan = maBienBan,
                    KyKiemKe = KyKiemKe,
                    PhamViKho = phamVi,
                    NguoiKiemKe = nguoiThucHien,
                    TongMatHang = ItemsKiemKe.Count,
                    SoMatHangLech = soMatHangLech,
                    GiaTriChenhLech = tongGiaTriLech,
                    GhiChu = string.IsNullOrWhiteSpace(GhiChuChung) ? "Hoàn tất kiểm kê định kỳ" : GhiChuChung.Trim(),
                    ThoiGian = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Đã chốt biên bản [{maBienBan}] ({KyKiemKe})! Tổng kiểm: {ItemsKiemKe.Count} mặt hàng, phát hiện lệch và tự động cân bằng: {soMatHangLech} mặt hàng.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi chốt kiểm kê kho.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi chốt biên bản kiểm kê kho.";
            }

            return RedirectToPage(new { LoaiVatTuFilter });
        }
    }
}
