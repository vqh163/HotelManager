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

        public QuanLyKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách vật tư hiển thị lên giao diện bảng
        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();

        // Thống kê tổng số mặt hàng hiện có
        public int TongSoMatHang { get; set; } = 0;

        // Thuộc tính nhận tham số lọc loại kho từ URL (Kho vật dụng, Kho nhà hàng, Kho đồ tiêu hao)
        [BindProperty(SupportsGet = true)]
        public string LoaiVatTuFilter { get; set; } = "ALL";

        // Thuộc tính nhận từ khóa tìm kiếm từ người dùng
        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        // Thuộc tính nhận dữ liệu từ Form Thêm mới / Cập nhật
        [BindProperty]
        public KHO_VAT_TU InputData { get; set; } = new KHO_VAT_TU();

        // Biến lưu thông báo trạng thái
        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        /// <summary>
        /// Xử lý tải danh sách hàng hóa tồn kho theo bộ lọc danh mục và từ khóa
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                // Đếm tổng số mặt hàng trong kho để hiển thị trên nút "Tất cả"
                TongSoMatHang = await _context.KHO_VAT_TU.CountAsync();

                // Khởi tạo truy vấn LINQ
                var query = _context.KHO_VAT_TU.AsNoTracking().AsQueryable();

                // Lọc theo 3 danh mục kho: Kho vật dụng, Kho nhà hàng, Kho đồ tiêu hao
                if (!string.IsNullOrEmpty(LoaiVatTuFilter) && LoaiVatTuFilter != "ALL")
                {
                    query = query.Where(v => v.LoaiVatTu == LoaiVatTuFilter);
                }

                // Lọc theo từ khóa tìm kiếm (Mã vật tư hoặc Tên vật tư)
                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    string keyword = TuKhoa.Trim();
                    query = query.Where(v =>
                        (v.TenVatTu != null && v.TenVatTu.Contains(keyword)) ||
                        v.MaVatTu.Contains(keyword));
                }

                // Sắp xếp ưu tiên hiển thị các mặt hàng sắp hết (tồn <= tối thiểu) lên đầu, sau đó theo tên
                DanhSachVatTu = await query
                    .OrderBy(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0) ? 0 : 1)
                    .ThenBy(v => v.TenVatTu)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải danh mục kho: " + ex.Message;
            }
        }

        /// <summary>
        /// Xử lý thêm mới vật tư vào hệ thống kho
        /// </summary>
        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                // Tự động sinh mã vật tư duy nhất (VD: VT261104120530)
                InputData.MaVatTu = "VT" + DateTime.Now.ToString("yyMMddHHmmss");

                // Đảm bảo các trường số không bị null khi lưu mới
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

            return RedirectToPage(new { LoaiVatTuFilter = this.LoaiVatTuFilter, TuKhoa = this.TuKhoa });
        }

        /// <summary>
        /// Xử lý cập nhật thông tin hàng hóa/vật tư trong kho
        /// </summary>
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            try
            {
                // Truy vấn tìm vật tư theo khóa chính bằng LINQ
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
                else
                {
                    ThongBaoLoi = "Không tìm thấy mã vật tư cần cập nhật trong cơ sở dữ liệu.";
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi cập nhật vật tư: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter = this.LoaiVatTuFilter, TuKhoa = this.TuKhoa });
        }

        /// <summary>
        /// Xử lý xóa vật tư khỏi danh mục kho
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
                else
                {
                    ThongBaoLoi = "Không tìm thấy vật tư cần xóa.";
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi khi vật tư đã được tham chiếu ở bảng CHI_TIET_DICH_VU hoặc PHIEU_BAO_CAO_SU_CO
                ThongBaoLoi = "Không thể xóa vật tư này vì đã phát sinh dữ liệu tiêu hao/sự cố liên quan: " + ex.Message;
            }

            return RedirectToPage(new { LoaiVatTuFilter = this.LoaiVatTuFilter, TuKhoa = this.TuKhoa });
        }
    }
}