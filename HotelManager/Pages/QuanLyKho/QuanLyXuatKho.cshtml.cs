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
    public class QuanLyXuatKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyXuatKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách vật tư còn tồn kho (> 0) để hiển thị trong Dropdown xuất kho
        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();

        // Danh sách lịch sử các phiếu xuất kho
        public IList<QuanLyNhapKhoModel.PhieuKhoViewModel> LichSuXuatKho { get; set; } = new List<QuanLyNhapKhoModel.PhieuKhoViewModel>();

        // Nhận dữ liệu từ Form lập phiếu xuất kho
        [BindProperty]
        public XuatKhoInputModel Input { get; set; } = new XuatKhoInputModel();

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

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

        /// <summary>
        /// Tải danh sách vật tư và lịch sử phiếu xuất kho
        /// </summary>
        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Truy vấn danh sách vật tư bằng LINQ
                DanhSachVatTu = await _context.KHO_VAT_TU
                    .AsNoTracking()
                    .OrderBy(v => v.TenVatTu)
                    .ToListAsync();

                // Đọc lịch sử các phiếu XUẤT từ file log giao dịch kho
                LichSuXuatKho = DocLichSuGiaoDich("XUAT");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải dữ liệu xuất kho: " + ex.Message;
            }
        }

        /// <summary>
        /// Xử lý xuất kho: Kiểm tra tồn kho khả dụng, tự động trừ định lượng và cảnh báo nếu chạm mức tối thiểu
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = "Vui lòng kiểm tra lại thông tin phiếu xuất kho.";
                    return Page();
                }

                // Tìm vật tư trong Database bằng LINQ
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == Input.MaVatTu);
                if (vatTuDb == null)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = "Không tìm thấy vật tư cần xuất trong cơ sở dữ liệu!";
                    return Page();
                }

                int tonHienTai = vatTuDb.SoLuongTon ?? 0;

                // Kiểm tra nghiệp vụ: Không cho phép xuất vượt quá số lượng tồn hiện tại
                if (Input.SoLuongXuat > tonHienTai)
                {
                    await LoadDataAsync();
                    ThongBaoLoi = $"Không đủ hàng để xuất! Vật tư '{vatTuDb.TenVatTu}' hiện chỉ còn tồn {tonHienTai} {vatTuDb.DonViTinh}.";
                    return Page();
                }

                // Tự động trừ định lượng tồn kho
                vatTuDb.SoLuongTon = tonHienTai - Input.SoLuongXuat;
                await _context.SaveChangesAsync();

                // Ghi log phiếu xuất kho vào file lichsu_nhapxuat_meta.txt
                string maPhieu = "PX" + DateTime.Now.ToString("yyMMddHHmmss");
                GhiLichSuGiaoDich(
                    maPhieu,
                    "XUAT",
                    vatTuDb.MaVatTu,
                    vatTuDb.TenVatTu ?? "Vật tư",
                    vatTuDb.LoaiVatTu ?? "Kho chung",
                    vatTuDb.DonViTinh ?? "Cái",
                    Input.SoLuongXuat,
                    vatTuDb.DonGiaNhap ?? 0,
                    Input.BoPhanNhan,
                    Input.LyDoXuat
                );

                // Cảnh báo nếu sau khi xuất, tồn kho chạm hoặc thấp hơn mức tối thiểu
                int tonToiThieu = vatTuDb.SoLuongTonToiThieu ?? 0;
                string canhBao = vatTuDb.SoLuongTon <= tonToiThieu
                    ? $" (CẢNH BÁO: Tồn kho còn {vatTuDb.SoLuongTon}, đã chạm mức tối thiểu {tonToiThieu} - Cần lên đề xuất mua hàng!)"
                    : $" (Tồn còn lại: {vatTuDb.SoLuongTon}).";

                ThongBao = $"Lập phiếu xuất [{maPhieu}] thành công! Đã xuất {Input.SoLuongXuat} {vatTuDb.DonViTinh} '{vatTuDb.TenVatTu}' cho {Input.BoPhanNhan}{canhBao}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi xuất kho: " + ex.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private void GhiLichSuGiaoDich(string maPhieu, string loai, string maVT, string tenVT, string loaiKho, string dvt, int soLuong, decimal donGia, string boPhan, string lyDo)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                string cleanBoPhan = boPhan.Replace("|", "/").Trim();
                string cleanLyDo = lyDo.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();
                string line = string.Join("|",
                    maPhieu,
                    loai,
                    maVT,
                    tenVT,
                    loaiKho,
                    dvt,
                    soLuong,
                    donGia.ToString(CultureInfo.InvariantCulture),
                    cleanBoPhan,
                    cleanLyDo,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ) + Environment.NewLine;

                System.IO.File.AppendAllText(filePath, line);
            }
            catch { }
        }

        private List<QuanLyNhapKhoModel.PhieuKhoViewModel> DocLichSuGiaoDich(string loaiCanLoc)
        {
            var list = new List<QuanLyNhapKhoModel.PhieuKhoViewModel>();
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

                        list.Add(new QuanLyNhapKhoModel.PhieuKhoViewModel
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