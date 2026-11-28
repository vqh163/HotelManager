using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    [Authorize]
    public class QuanLyDonDatHangModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private static readonly object _poFileLock = new object();

        public QuanLyDonDatHangModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string CheDoHienThi { get; set; } = "CAN_NHAP";

        public int SoMatHangCanNhap { get; set; } = 0;

        [BindProperty]
        public string NhaCungCapPO { get; set; } = string.Empty;

        [BindProperty]
        public string NguoiLapPO { get; set; } = string.Empty;

        [BindProperty]
        public string? GhiChuPO { get; set; }

        [BindProperty]
        public List<DeXuatMuaHangItem> DanhSachDeXuat { get; set; } = new List<DeXuatMuaHangItem>();

        public List<DonDatHangPOViewModel> DanhSachDonPO { get; set; } = new List<DonDatHangPOViewModel>();

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public class DeXuatMuaHangItem
        {
            public bool DuocChon { get; set; } = true;
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int TonHienTai { get; set; }
            public int TonToiThieu { get; set; }
            public int SoLuongDeXuat { get; set; }
            public decimal DonGiaDuKien { get; set; }
        }

        public class DonDatHangPOViewModel
        {
            public string MaPO { get; set; } = string.Empty;
            public string NhaCungCap { get; set; } = string.Empty;
            public string NguoiLap { get; set; } = string.Empty;
            public string TomTatMatHang { get; set; } = string.Empty;
            public decimal TongTienDuToan { get; set; }
            public string TrangThai { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
            public DateTime NgayLap { get; set; }
            public string DuLieuChiTietRaw { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            try
            {
                NguoiLapPO = User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)
                    ? User.Identity.Name
                    : "Thủ kho";

                var allVatTu = await _context.KHO_VAT_TU
                    .AsNoTracking()
                    .OrderBy(v => v.LoaiVatTu)
                    .ThenBy(v => v.TenVatTu)
                    .ToListAsync();

                SoMatHangCanNhap = allVatTu.Count(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0));

                var filtered = CheDoHienThi == "ALL"
                    ? allVatTu
                    : allVatTu.Where(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0)).ToList();

                DanhSachDeXuat = filtered.Select(v =>
                {
                    int ton = v.SoLuongTon ?? 0;
                    int toiThieu = v.SoLuongTonToiThieu ?? 0;

                    int mucAnToan = toiThieu > 0 ? toiThieu * 2 : 20;
                    int goiY = mucAnToan - ton;
                    if (goiY <= 0) goiY = 10;

                    return new DeXuatMuaHangItem
                    {
                        DuocChon = ton <= toiThieu,
                        MaVatTu = v.MaVatTu,
                        TenVatTu = v.TenVatTu ?? "Vật tư",
                        LoaiVatTu = v.LoaiVatTu ?? "Kho chung",
                        DonViTinh = v.DonViTinh ?? "Cái",
                        TonHienTai = ton,
                        TonToiThieu = toiThieu,
                        SoLuongDeXuat = goiY,
                        DonGiaDuKien = v.DonGiaNhap ?? 0
                    };
                }).ToList();

                DanhSachDonPO = DocDanhSachPO();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi sinh đề xuất mua hàng tự động: " + ex.Message;
            }
        }

        public IActionResult OnPostTaoDonPO()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NhaCungCapPO))
                {
                    ThongBaoLoi = "Vui lòng nhập tên nhà cung cấp / đối tác cung ứng.";
                    return RedirectToPage(new { CheDoHienThi });
                }

                if (string.IsNullOrWhiteSpace(NguoiLapPO))
                {
                    NguoiLapPO = User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)
                        ? User.Identity.Name
                        : "Thủ kho";
                }

                var cacMucChon = DanhSachDeXuat?.Where(x => x.DuocChon).ToList();
                if (cacMucChon == null || !cacMucChon.Any())
                {
                    ThongBaoLoi = "Vui lòng tích chọn ít nhất 1 mặt hàng cần đặt mua để lên đơn PO!";
                    return RedirectToPage(new { CheDoHienThi });
                }

                if (cacMucChon.Any(x => x.SoLuongDeXuat <= 0 || x.DonGiaDuKien < 0))
                {
                    ThongBaoLoi = "Số lượng đặt mua phải lớn hơn 0 và đơn giá dự kiến không được âm.";
                    return RedirectToPage(new { CheDoHienThi });
                }

                string maPO = "PO" + DateTime.Now.ToString("yyMMddHHmmss") + Random.Shared.Next(10, 99);
                decimal tongDuToan = cacMucChon.Sum(x => x.SoLuongDeXuat * x.DonGiaDuKien);

                var tomTatList = cacMucChon.Select(x => $"{x.TenVatTu} ({x.SoLuongDeXuat} {x.DonViTinh})");
                string tomTat = string.Join(", ", tomTatList);

                var rawItems = cacMucChon.Select(x => $"{x.MaVatTu}^{x.SoLuongDeXuat}^{x.DonGiaDuKien.ToString(CultureInfo.InvariantCulture)}");
                string rawData = string.Join(";", rawItems);

                LuuDonDatHangPO(
                    maPO,
                    NhaCungCapPO,
                    NguoiLapPO,
                    tomTat,
                    tongDuToan,
                    "Chờ giao hàng",
                    GhiChuPO ?? "",
                    rawData
                );

                ThongBao = $"Đã phát hành Đơn đặt hàng [{maPO}] gồm {cacMucChon.Count} mặt hàng gửi tới '{NhaCungCapPO.Trim()}' (Tổng dự toán: {tongDuToan:N0} ₫).";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tạo đơn đặt hàng (PO): " + ex.Message;
            }

            return RedirectToPage(new { CheDoHienThi });
        }

        private void LuuDonDatHangPO(string maPO, string ncc, string nguoiLap, string tomTat, decimal tongTien, string trangThai, string ghiChu, string rawData)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_dondathang_po_meta.txt");
            string line = string.Join("|",
                maPO,
                ncc.Replace("|", "/").Trim(),
                nguoiLap.Replace("|", "/").Trim(),
                tomTat.Replace("|", "/").Trim(),
                tongTien.ToString(CultureInfo.InvariantCulture),
                trangThai,
                ghiChu.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim(),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                rawData
            ) + Environment.NewLine;

            lock (_poFileLock)
            {
                System.IO.File.AppendAllText(filePath, line);
            }
        }

        private List<DonDatHangPOViewModel> DocDanhSachPO()
        {
            var list = new List<DonDatHangPOViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_dondathang_po_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                string[] lines;
                lock (_poFileLock)
                {
                    lines = System.IO.File.ReadAllLines(filePath);
                }

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var p = line.Split('|');
                    if (p.Length >= 9)
                    {
                        decimal.TryParse(p[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tong);
                        bool okNgay = DateTime.TryParseExact(
                                          p[7].Trim(),
                                          "yyyy-MM-dd HH:mm:ss",
                                          CultureInfo.InvariantCulture,
                                          DateTimeStyles.None,
                                          out DateTime ngay)
                                      || DateTime.TryParse(p[7].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out ngay);

                        list.Add(new DonDatHangPOViewModel
                        {
                            MaPO = p[0].Trim(),
                            NhaCungCap = p[1].Trim(),
                            NguoiLap = p[2].Trim(),
                            TomTatMatHang = p[3].Trim(),
                            TongTienDuToan = tong,
                            TrangThai = p[5].Trim(),
                            GhiChu = p[6].Trim(),
                            NgayLap = okNgay ? ngay : DateTime.MinValue,
                            DuLieuChiTietRaw = p[8].Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Không thể đọc danh sách đơn đặt hàng (PO): " + ex.Message;
            }

            return list
                .OrderBy(x => x.TrangThai == "Đã nhận hàng & Nhập kho" ? 1 : 0)
                .ThenByDescending(x => x.NgayLap)
                .ToList();
        }
    }
}