using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
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

        public QuanLyKhoModel(ApplicationDbContext context) => _context = context;

        // Danh sách vật tư hiển thị ở bảng chính bên trái
        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();

        // Danh sách vật tư chạm ngưỡng tối thiểu hiển thị ở cột phụ bên phải
        public IList<KHO_VAT_TU> DanhSachCanhBao { get; set; } = new List<KHO_VAT_TU>();

        // Các chỉ số KPI tổng quan
        public int TongSoMatHang { get; set; }
        public decimal TongGiaTriTonKho { get; set; }
        public int SoMatHangSapHet { get; set; }
        public int SoMatHangHetSach { get; set; }

        // Số lượng mặt hàng theo từng phân khu kho
        public int DemKhoVatDung { get; set; }
        public int DemKhoNhaHang { get; set; }
        public int DemKhoTieuHao { get; set; }

        // Giá trị vốn tồn kho theo từng phân khu
        public decimal GiaTriKhoVatDung { get; set; }
        public decimal GiaTriKhoNhaHang { get; set; }
        public decimal GiaTriKhoTieuHao { get; set; }

        // Các thuộc tính Binding phục vụ lọc và tìm kiếm
        [BindProperty(SupportsGet = true)]
        public string LoaiVatTuFilter { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public string TrangThaiTonFilter { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        // Đối tượng nhận dữ liệu từ Form Modal Thêm / Sửa
        [BindProperty]
        public KHO_VAT_TU InputData { get; set; } = new KHO_VAT_TU();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        /// <summary>
        /// Truy xuất dữ liệu từ bảng KHO_VAT_TU bằng LINQ, tính toán KPI và lọc danh sách hiển thị
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                var toanBoKho = await _context.KHO_VAT_TU.AsNoTracking().ToListAsync();

                // Tính toán 4 chỉ số KPI phía trên
                TongSoMatHang = toanBoKho.Count;
                TongGiaTriTonKho = toanBoKho.Sum(v => (decimal)(v.SoLuongTon ?? 0) * (v.DonGiaNhap ?? 0));
                SoMatHangSapHet = toanBoKho.Count(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0));
                SoMatHangHetSach = toanBoKho.Count(v => (v.SoLuongTon ?? 0) <= 0);

                // Đếm số mặt hàng và tính giá trị vốn theo 3 phân khu
                DemKhoVatDung = toanBoKho.Count(v => v.LoaiVatTu == "Kho vật dụng");
                DemKhoNhaHang = toanBoKho.Count(v => v.LoaiVatTu == "Kho nhà hàng");
                DemKhoTieuHao = toanBoKho.Count(v => v.LoaiVatTu == "Kho đồ tiêu hao");

                GiaTriKhoVatDung = toanBoKho.Where(v => v.LoaiVatTu == "Kho vật dụng").Sum(v => (decimal)(v.SoLuongTon ?? 0) * (v.DonGiaNhap ?? 0));
                GiaTriKhoNhaHang = toanBoKho.Where(v => v.LoaiVatTu == "Kho nhà hàng").Sum(v => (decimal)(v.SoLuongTon ?? 0) * (v.DonGiaNhap ?? 0));
                GiaTriKhoTieuHao = toanBoKho.Where(v => v.LoaiVatTu == "Kho đồ tiêu hao").Sum(v => (decimal)(v.SoLuongTon ?? 0) * (v.DonGiaNhap ?? 0));

                // Lọc danh sách cảnh báo cho cột bên phải
                DanhSachCanhBao = toanBoKho
                    .Where(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0))
                    .OrderBy(v => v.SoLuongTon ?? 0)
                    .ToList();

                // Lọc dữ liệu cho bảng chính bằng LINQ
                var query = toanBoKho.AsQueryable();

                if (!string.IsNullOrEmpty(LoaiVatTuFilter) && LoaiVatTuFilter != "ALL")
                {
                    query = query.Where(v => v.LoaiVatTu == LoaiVatTuFilter);
                }

                if (!string.IsNullOrEmpty(TrangThaiTonFilter) && TrangThaiTonFilter != "ALL")
                {
                    if (TrangThaiTonFilter == "AN_TOAN")
                        query = query.Where(v => (v.SoLuongTon ?? 0) > (v.SoLuongTonToiThieu ?? 0));
                    else if (TrangThaiTonFilter == "SAP_HET")
                        query = query.Where(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0));
                    else if (TrangThaiTonFilter == "HET_HANG")
                        query = query.Where(v => (v.SoLuongTon ?? 0) <= 0);
                }

                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    string keyword = TuKhoa.Trim();
                    query = query.Where(v =>
                        (v.TenVatTu != null && v.TenVatTu.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        v.MaVatTu.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (v.DonViTinh != null && v.DonViTinh.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                }

                DanhSachVatTu = query
                    .OrderBy(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0) ? 0 : 1)
                    .ThenBy(v => v.TenVatTu)
                    .ToList();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải danh mục kho: " + ex.Message;
            }
        }

        /// <summary>
        /// Thêm mới vật tư vào CSDL bọc trong khối try...catch
        /// </summary>
        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                InputData.MaVatTu = "VT" + DateTime.Now.ToString("yyMMddHHmmss");
                InputData.SoLuongTon = InputData.SoLuongTon ?? 0;
                InputData.SoLuongTonToiThieu = InputData.SoLuongTonToiThieu ?? 0;
                InputData.DonGiaNhap = InputData.DonGiaNhap ?? 0;

                _context.KHO_VAT_TU.Add(InputData);
                await _context.SaveChangesAsync();

                ThongBao = $"Đã thêm mới hàng hóa '{InputData.TenVatTu}' vào {InputData.LoaiVatTu}!";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi thêm mới vật tư: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter, TrangThaiTonFilter, TuKhoa });
        }

        /// <summary>
        /// Cập nhật thông tin vật tư bằng LINQ
        /// </summary>
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            try
            {
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == InputData.MaVatTu);
                if (vatTuDb != null)
                {
                    vatTuDb.TenVatTu = InputData.TenVatTu;
                    vatTuDb.LoaiVatTu = InputData.LoaiVatTu;
                    vatTuDb.DonViTinh = InputData.DonViTinh;
                    vatTuDb.DonGiaNhap = InputData.DonGiaNhap ?? 0;
                    vatTuDb.SoLuongTon = InputData.SoLuongTon ?? 0;
                    vatTuDb.SoLuongTonToiThieu = InputData.SoLuongTonToiThieu ?? 0;

                    await _context.SaveChangesAsync();
                    ThongBao = $"Đã cập nhật thông tin vật tư '{vatTuDb.TenVatTu}' thành công!";
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi cập nhật vật tư: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter, TrangThaiTonFilter, TuKhoa });
        }

        /// <summary>
        /// Xử lý nhập bù nhanh 1-click cho các vật tư chạm định mức tối thiểu
        /// </summary>
        public async Task<IActionResult> OnPostNhapBuNhanhAsync(string maVatTu, int soLuongBu)
        {
            try
            {
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == maVatTu);
                if (vatTuDb != null && soLuongBu > 0)
                {
                    vatTuDb.SoLuongTon = (vatTuDb.SoLuongTon ?? 0) + soLuongBu;
                    await _context.SaveChangesAsync();

                    ThongBao = $"Đã nhập bù nhanh +{soLuongBu} {vatTuDb.DonViTinh} cho '{vatTuDb.TenVatTu}' (Tồn mới: {vatTuDb.SoLuongTon})!";
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi nhập bù nhanh: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter, TrangThaiTonFilter, TuKhoa });
        }

        /// <summary>
        /// Xóa vật tư khỏi danh mục kho an toàn
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                var vatTuDb = await _context.KHO_VAT_TU.FirstOrDefaultAsync(v => v.MaVatTu == id);
                if (vatTuDb != null)
                {
                    string tenTam = vatTuDb.TenVatTu ?? id;
                    _context.KHO_VAT_TU.Remove(vatTuDb);
                    await _context.SaveChangesAsync();
                    ThongBao = $"Đã xóa vật tư '{tenTam}' khỏi danh mục kho.";
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Không thể xóa do vướng dữ liệu liên kết: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter, TrangThaiTonFilter, TuKhoa });
        }
    }
}