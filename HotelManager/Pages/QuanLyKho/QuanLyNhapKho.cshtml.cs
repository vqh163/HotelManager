using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    public class QuanLyNhapKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyNhapKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách vật tư nạp vào Dropdown để chọn nhập kho
        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();

        // Danh sách lịch sử các phiếu nhập kho
        public IList<PhieuKhoViewModel> LichSuNhapKho { get; set; } = new List<PhieuKhoViewModel>();

        // Nhận dữ liệu từ Form lập phiếu nhập kho
        [BindProperty]
        public NhapKhoInputModel Input { get; set; } = new NhapKhoInputModel();

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

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
            public string LoaiGiaoDich { get; set; } = string.Empty; // NHAP hoặc XUAT
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

        /// <summary>
        /// Tải danh sách vật tư và lịch sử phiếu nhập kho
        /// </summary>
        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Truy vấn danh sách vật tư bằng LINQ, sắp xếp theo tên
                DanhSachVatTu = await _context.KHO_VAT_TU
                    .AsNoTracking()
                    .OrderBy(v => v.TenVatTu)
                    .ToListAsync();

                // Đọc lịch sử các phiếu NHẬP từ file log giao dịch kho
                LichSuNhapKho = DocLichSuGiaoDich("NHAP");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải dữ liệu nhập kho: " + ex.Message;
            }
        }

        /// <summary>
        /// Xử lý lưu phiếu nhập kho và cộng dồn số lượng tồn trong bảng KHO_VAT_TU
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = "Vui lòng kiểm tra lại thông tin phiếu nhập.";
                    return Page();
                }

                // Tìm vật tư trong Database bằng LINQ
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == Input.MaVatTu);
                if (vatTuDb == null)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = "Không tìm thấy vật tư trong cơ sở dữ liệu!";
                    return Page();
                }

                // Cộng dồn số lượng tồn kho và cập nhật đơn giá nhập mới nhất
                int tonCu = vatTuDb.SoLuongTon ?? 0;
                vatTuDb.SoLuongTon = tonCu + Input.SoLuongNhap;
                vatTuDb.DonGiaNhap = Input.DonGiaNhapMoi;

                await _context.SaveChangesAsync();

                // Ghi log phiếu nhập kho vào file lichsu_nhapxuat_meta.txt để theo dõi
                string maPhieu = "PN" + DateTime.Now.ToString("yyMMddHHmmss");
                GhiLichSuGiaoDich(
                    maPhieu,
                    "NHAP",
                    vatTuDb.MaVatTu,
                    vatTuDb.TenVatTu ?? "Vật tư",
                    vatTuDb.LoaiVatTu ?? "Kho chung",
                    vatTuDb.DonViTinh ?? "Cái",
                    Input.SoLuongNhap,
                    Input.DonGiaNhapMoi,
                    Input.NhaCungCap,
                    Input.GhiChu ?? ""
                );

                ThongBao = $"Lập phiếu nhập [{maPhieu}] thành công! Đã cộng thêm {Input.SoLuongNhap} {vatTuDb.DonViTinh} '{vatTuDb.TenVatTu}' vào kho (Tồn mới: {vatTuDb.SoLuongTon}).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi nhập kho: " + ex.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private void GhiLichSuGiaoDich(string maPhieu, string loai, string maVT, string tenVT, string loaiKho, string dvt, int soLuong, decimal donGia, string doiTac, string ghiChu)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                string cleanDoiTac = doiTac.Replace("|", "/").Trim();
                string cleanGhiChu = ghiChu.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();
                string line = string.Join("|",
                    maPhieu,
                    loai,
                    maVT,
                    tenVT,
                    loaiKho,
                    dvt,
                    soLuong,
                    donGia.ToString(CultureInfo.InvariantCulture),
                    cleanDoiTac,
                    cleanGhiChu,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ) + Environment.NewLine;

                System.IO.File.AppendAllText(filePath, line);
            }
            catch { }
        }

        private List<PhieuKhoViewModel> DocLichSuGiaoDich(string loaiCanLoc)
        {
            var list = new List<PhieuKhoViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var p = line.Split('|');
                    if (p.Length >= 11 && p[1] == loaiCanLoc)
                    {
                        int.TryParse(p[6], out int sl);
                        decimal.TryParse(p[7], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal gia);
                        DateTime.TryParse(p[10], out DateTime tg);

                        list.Add(new PhieuKhoViewModel
                        {
                            MaPhieu = p[0],
                            LoaiGiaoDich = p[1],
                            MaVatTu = p[2],
                            TenVatTu = p[3],
                            LoaiKho = p[4],
                            DonViTinh = p[5],
                            SoLuong = sl,
                            DonGia = gia,
                            DoiTacHoacBoPhan = p[8],
                            GhiChu = p[9],
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