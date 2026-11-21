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
    public class KiemKeKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public KiemKeKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lọc theo danh mục kho cần kiểm kê (ALL, Kho vật dụng, Kho nhà hàng, Kho đồ tiêu hao)
        [BindProperty(SupportsGet = true)]
        public string LoaiVatTuFilter { get; set; } = "ALL";

        // Thông tin chung của biên bản kiểm kê định kỳ
        [BindProperty]
        public string KyKiemKe { get; set; } = "Kiểm kê định kỳ Hàng Tuần";

        [BindProperty]
        public string NguoiKiemKe { get; set; } = string.Empty;

        [BindProperty]
        public string? GhiChuChung { get; set; }

        // Danh sách chi tiết từng mặt hàng trong bảng kiểm kê
        [BindProperty]
        public List<KiemKeItemInput> ItemsKiemKe { get; set; } = new List<KiemKeItemInput>();

        // Lịch sử các biên bản kiểm kê đã thực hiện
        public List<BienBanKiemKeViewModel> LichSuKiemKe { get; set; } = new List<BienBanKiemKeViewModel>();

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

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

        /// <summary>
        /// Tải danh sách vật tư cần kiểm kê theo bộ lọc kho và nạp lịch sử kiểm kê
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                // Tự động điền tên người kiểm kê nếu đã đăng nhập
                NguoiKiemKe = User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)
                    ? User.Identity.Name
                    : "Thủ kho ca trực";

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
                    TonThucTe = v.SoLuongTon ?? 0, // Mặc định gán bằng tồn hệ thống để thủ kho chỉ cần sửa dòng bị lệch
                    LyDoChenhLech = ""
                }).ToList();

                LichSuKiemKe = DocLichSuKiemKe();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải danh sách kiểm kê: " + ex.Message;
            }
        }

        /// <summary>
        /// Xử lý chốt biên bản kiểm kê định kỳ: Tự động cân bằng số lượng tồn trong bảng KHO_VAT_TU và lưu vết
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (ItemsKiemKe == null || !ItemsKiemKe.Any())
                {
                    ThongBaoLoi = "Không có dữ liệu vật tư nào để chốt kiểm kê.";
                    return RedirectToPage(new { LoaiVatTuFilter = this.LoaiVatTuFilter });
                }

                string maBienBan = "KK" + DateTime.Now.ToString("yyMMddHHmmss");
                int soMatHangLech = 0;
                decimal tongGiaTriLech = 0;

                // Lấy toàn bộ danh sách vật tư từ DB lên để đối chiếu và cập nhật bằng LINQ
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

                        // Tự động cân bằng lại tồn kho thực tế vào bảng KHO_VAT_TU
                        vtDb.SoLuongTon = tonMoi;

                        // Ghi đồng bộ vào lịch sử Nhập/Xuất để phục vụ truy vết và báo cáo tiêu hao (UC_12)
                        string loaiGD = chenhLech > 0 ? "NHAP" : "XUAT";
                        string dienGiai = $"[Cân bằng {KyKiemKe} - {maBienBan}] " +
                                          (string.IsNullOrWhiteSpace(item.LyDoChenhLech) ? "Điều chỉnh chênh lệch kiểm kê" : item.LyDoChenhLech);

                        GhiLichSuDieuChinhKho(
                            maBienBan,
                            loaiGD,
                            vtDb.MaVatTu,
                            vtDb.TenVatTu ?? "Vật tư",
                            vtDb.LoaiVatTu ?? "Kho chung",
                            vtDb.DonViTinh ?? "Cái",
                            Math.Abs(chenhLech),
                            donGia,
                            $"Kiểm kê ({NguoiKiemKe})",
                            dienGiai
                        );
                    }
                }

                // Lưu thay đổi tồn kho vào SQL Server
                await _context.SaveChangesAsync();

                // Lưu biên bản kiểm kê vào file lichsu_kiemke_meta.txt
                string phamVi = string.IsNullOrEmpty(LoaiVatTuFilter) || LoaiVatTuFilter == "ALL" ? "Tất cả kho" : LoaiVatTuFilter;
                GhiBienBanKiemKe(
                    maBienBan,
                    KyKiemKe,
                    phamVi,
                    NguoiKiemKe,
                    ItemsKiemKe.Count,
                    soMatHangLech,
                    tongGiaTriLech,
                    GhiChuChung ?? "Hoàn tất kiểm kê định kỳ"
                );

                ThongBao = $"Đã chốt biên bản [{maBienBan}] ({KyKiemKe})! Tổng kiểm: {ItemsKiemKe.Count} mặt hàng, phát hiện lệch và tự động cân bằng: {soMatHangLech} mặt hàng.";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi chốt kiểm kê kho: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter = this.LoaiVatTuFilter });
        }

        private void GhiBienBanKiemKe(string maBB, string kyKK, string phamVi, string nguoiKK, int tongMH, int soLech, decimal giaTriLech, string ghiChu)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_kiemke_meta.txt");
                string cleanNguoi = nguoiKK.Replace("|", "/").Trim();
                string cleanGhiChu = ghiChu.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();

                string line = string.Join("|",
                    maBB,
                    kyKK,
                    phamVi,
                    cleanNguoi,
                    tongMH,
                    soLech,
                    giaTriLech.ToString(CultureInfo.InvariantCulture),
                    cleanGhiChu,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ) + Environment.NewLine;

                System.IO.File.AppendAllText(filePath, line);
            }
            catch { }
        }

        private void GhiLichSuDieuChinhKho(string maPhieu, string loai, string maVT, string tenVT, string loaiKho, string dvt, int soLuong, decimal donGia, string doiTac, string ghiChu)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                string line = string.Join("|",
                    maPhieu,
                    loai,
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

        private List<BienBanKiemKeViewModel> DocLichSuKiemKe()
        {
            var list = new List<BienBanKiemKeViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_kiemke_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var p = line.Split('|');
                    if (p.Length >= 9)
                    {
                        int.TryParse(p[4], out int tongMH);
                        int.TryParse(p[5], out int soLech);
                        decimal.TryParse(p[6], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal giaTri);
                        DateTime.TryParse(p[8], out DateTime tg);

                        list.Add(new BienBanKiemKeViewModel
                        {
                            MaBienBan = p[0],
                            KyKiemKe = p[1],
                            PhamViKho = p[2],
                            NguoiKiemKe = p[3],
                            TongMatHang = tongMH,
                            SoMatHangLech = soLech,
                            GiaTriChenhLech = giaTri,
                            GhiChu = p[7],
                            ThoiGian = tg
                        });
                    }
                }
            }
            catch { }

            return list.OrderByDescending(x => x.ThoiGian).ToList();
        }
    }
}