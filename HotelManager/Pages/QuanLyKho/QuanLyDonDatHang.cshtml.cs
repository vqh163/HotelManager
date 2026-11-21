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
    public class QuanLyDonDatHangModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyDonDatHangModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Chế độ lọc: CAN_NHAP (chỉ hiện vật tư tồn <= tối thiểu) hoặc ALL (toàn bộ kho)
        [BindProperty(SupportsGet = true)]
        public string CheDoHienThi { get; set; } = "CAN_NHAP";

        public int SoMatHangCanNhap { get; set; } = 0;

        // Thông tin chung khi lập đơn đặt hàng PO
        [BindProperty]
        public string NhaCungCapPO { get; set; } = string.Empty;

        [BindProperty]
        public string NguoiLapPO { get; set; } = string.Empty;

        [BindProperty]
        public string? GhiChuPO { get; set; }

        // Danh sách mặt hàng đề xuất mua tự động
        [BindProperty]
        public List<DeXuatMuaHangItem> DanhSachDeXuat { get; set; } = new List<DeXuatMuaHangItem>();

        // Danh sách các đơn đặt hàng (PO) đã phát hành
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

        /// <summary>
        /// Quét bảng KHO_VAT_TU bằng LINQ để sinh danh sách đề xuất mua hàng tự động
        /// </summary>
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

                // Đếm số lượng mặt hàng chạm hoặc dưới ngưỡng tối thiểu
                SoMatHangCanNhap = allVatTu.Count(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0));

                var filtered = CheDoHienThi == "ALL"
                    ? allVatTu
                    : allVatTu.Where(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0)).ToList();

                DanhSachDeXuat = filtered.Select(v =>
                {
                    int ton = v.SoLuongTon ?? 0;
                    int toiThieu = v.SoLuongTonToiThieu ?? 0;

                    // Công thức tự động đề xuất số lượng cần mua: Bù lên gấp đôi mức tối thiểu (tối thiểu gợi ý 10 đơn vị)
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

        /// <summary>
        /// Xử lý phát hành Đơn Đặt Hàng (PO) từ các mặt hàng được chọn
        /// </summary>
        public IActionResult OnPostTaoDonPO()
        {
            try
            {
                var cacMucChon = DanhSachDeXuat?.Where(x => x.DuocChon && x.SoLuongDeXuat > 0).ToList();
                if (cacMucChon == null || !cacMucChon.Any())
                {
                    ThongBaoLoi = "Vui lòng tích chọn ít nhất 1 mặt hàng cần đặt mua để lên đơn PO!";
                    return RedirectToPage(new { CheDoHienThi = this.CheDoHienThi });
                }

                string maPO = "PO" + DateTime.Now.ToString("yyMMddHHmmss");
                decimal tongDuToan = cacMucChon.Sum(x => x.SoLuongDeXuat * x.DonGiaDuKien);

                // Tạo chuỗi tóm tắt mặt hàng hiển thị và chuỗi chi tiết phục vụ nghiệm thu tự động
                var tomTatList = cacMucChon.Select(x => $"{x.TenVatTu} ({x.SoLuongDeXuat} {x.DonViTinh})");
                string tomTat = string.Join(", ", tomTatList);

                // Chuỗi chi tiết dạng: MaVatTu^SoLuong^DonGia;MaVatTu^SoLuong^DonGia
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

                ThongBao = $"Đã phát hành Đơn đặt hàng [{maPO}] gồm {cacMucChon.Count} mặt hàng gửi tới '{NhaCungCapPO}' (Tổng dự toán: {tongDuToan:N0} ₫).";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tạo đơn đặt hàng (PO): " + ex.Message;
            }

            return RedirectToPage(new { CheDoHienThi = this.CheDoHienThi });
        }

        /// <summary>
        /// Nghiệm thu Đơn Đặt Hàng (PO): Tự động cộng số lượng hàng về vào bảng KHO_VAT_TU và ghi lịch sử nhập kho
        /// </summary>
        public async Task<IActionResult> OnPostNghiemThuPOAsync(string maPO)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_dondathang_po_meta.txt");
                if (!System.IO.File.Exists(filePath))
                {
                    ThongBaoLoi = "Không tìm thấy dữ liệu đơn đặt hàng PO.";
                    return RedirectToPage();
                }

                var lines = System.IO.File.ReadAllLines(filePath).ToList();
                bool timThay = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    var p = lines[i].Split('|');
                    if (p.Length >= 9 && p[0] == maPO && p[5] != "Đã nhận hàng & Nhập kho")
                    {
                        timThay = true;
                        string nhaCungCap = p[1];
                        string rawItems = p[8];

                        // Phân tách từng mặt hàng trong đơn PO để cộng tồn kho bằng LINQ
                        var itemTokens = rawItems.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var token in itemTokens)
                        {
                            var sub = token.Split('^');
                            if (sub.Length >= 3)
                            {
                                string maVT = sub[0];
                                int.TryParse(sub[1], out int slNhap);
                                decimal.TryParse(sub[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal donGia);

                                var vtDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == maVT);
                                if (vtDb != null && slNhap > 0)
                                {
                                    vtDb.SoLuongTon = (vtDb.SoLuongTon ?? 0) + slNhap;
                                    if (donGia > 0) vtDb.DonGiaNhap = donGia;

                                    // Ghi đồng bộ vào lịch sử nhập xuất kho để Báo cáo tiêu hao/chi phí tổng hợp được
                                    GhiLichSuNhapKhoTuPO(
                                        "PN_" + maPO,
                                        vtDb.MaVatTu,
                                        vtDb.TenVatTu ?? "Vật tư",
                                        vtDb.LoaiVatTu ?? "Kho chung",
                                        vtDb.DonViTinh ?? "Cái",
                                        slNhap,
                                        donGia,
                                        nhaCungCap,
                                        $"Nghiệm thu nhập kho từ đơn {maPO}"
                                    );
                                }
                            }
                        }

                        await _context.SaveChangesAsync();

                        // Cập nhật trạng thái PO thành Đã nhận hàng & Nhập kho
                        p[5] = "Đã nhận hàng & Nhập kho";
                        lines[i] = string.Join("|", p);
                        break;
                    }
                }

                if (timThay)
                {
                    System.IO.File.WriteAllLines(filePath, lines);
                    ThongBao = $"Đã nghiệm thu đơn [{maPO}] thành công! Toàn bộ số lượng hàng hóa đã được tự động cộng vào Tồn kho.";
                }
                else
                {
                    ThongBaoLoi = "Đơn PO không tồn tại hoặc đã được nghiệm thu trước đó.";
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi nghiệm thu đơn PO: " + ex.Message;
            }

            return RedirectToPage(new { CheDoHienThi = this.CheDoHienThi });
        }

        private void LuuDonDatHangPO(string maPO, string ncc, string nguoiLap, string tomTat, decimal tongTien, string trangThai, string ghiChu, string rawData)
        {
            try
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
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    rawData
                ) + Environment.NewLine;

                System.IO.File.AppendAllText(filePath, line);
            }
            catch { }
        }

        private void GhiLichSuNhapKhoTuPO(string maPhieu, string maVT, string tenVT, string loaiKho, string dvt, int soLuong, decimal donGia, string doiTac, string ghiChu)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                string line = string.Join("|",
                    maPhieu,
                    "NHAP",
                    maVT,
                    tenVT,
                    loaiKho,
                    dvt,
                    soLuong,
                    donGia.ToString(CultureInfo.InvariantCulture),
                    doiTac.Replace("|", "/"),
                    ghiChu.Replace("|", "/"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ) + Environment.NewLine;

                System.IO.File.AppendAllText(filePath, line);
            }
            catch { }
        }

        private List<DonDatHangPOViewModel> DocDanhSachPO()
        {
            var list = new List<DonDatHangPOViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_dondathang_po_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var p = line.Split('|');
                    if (p.Length >= 9)
                    {
                        decimal.TryParse(p[4], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tong);
                        DateTime.TryParse(p[7], out DateTime ngay);

                        list.Add(new DonDatHangPOViewModel
                        {
                            MaPO = p[0],
                            NhaCungCap = p[1],
                            NguoiLap = p[2],
                            TomTatMatHang = p[3],
                            TongTienDuToan = tong,
                            TrangThai = p[5],
                            GhiChu = p[6],
                            NgayLap = ngay,
                            DuLieuChiTietRaw = p[8]
                        });
                    }
                }
            }
            catch { }

            return list.OrderByDescending(x => x.NgayLap).ToList();
        }
    }
}