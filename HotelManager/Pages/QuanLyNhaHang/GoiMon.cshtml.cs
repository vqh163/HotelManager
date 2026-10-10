using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class DichVuViewModel
    {
        public string MaDichVu { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string LoaiDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public bool CanhBaoDiUng { get; set; }
        public bool HetMon { get; set; }
    }

    public class CartItemDTO
    {
        public string MaDichVu { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public bool CoDiUng { get; set; }
    }

    public class GoiMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GoiMonModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public BAN_NHA_HANG BanNhaHang { get; set; } = new BAN_NHA_HANG();
        public List<DichVuViewModel> DanhSachMonAn { get; set; } = new List<DichVuViewModel>();

        [BindProperty]
        public string CartDataJson { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? DanhMucLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                // Truy vấn thông tin bàn
                BanNhaHang = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
                if (BanNhaHang == null) return RedirectToPage("./SoDoBan");

                var query = _context.DICH_VU.AsQueryable();

                // Lọc theo từ khóa tìm kiếm
                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    query = query.Where(d => d.TenDichVu.Contains(TuKhoaTimKiem) || d.MaDichVu.Contains(TuKhoaTimKiem));
                }

                // Lọc theo Tab danh mục
                if (!string.IsNullOrEmpty(DanhMucLoc) && DanhMucLoc != "Tất cả")
                {
                    query = query.Where(d => d.LoaiDichVu == DanhMucLoc);
                }

                var dsDichVu = await query.ToListAsync();

                string[] foodImages = {
                    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&q=80",
                    "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=300&q=80",
                    "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=300&q=80"
                };

                DanhSachMonAn = dsDichVu.Select((d, index) => new DichVuViewModel
                {
                    MaDichVu = d.MaDichVu,
                    TenDichVu = d.TenDichVu,
                    LoaiDichVu = string.IsNullOrEmpty(d.LoaiDichVu) ? "Món chính" : d.LoaiDichVu,
                    DonGia = d.DonGia ?? 0m,
                    HinhAnh = !string.IsNullOrEmpty(d.HinhAnh) ? d.HinhAnh : foodImages[index % foodImages.Length],
                    MoTa = "Mô tả nguyên liệu thành phần món ăn...",
                    CanhBaoDiUng = (index % 4 == 0),
                    HetMon = false
                }).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(CartDataJson))
                {
                    ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống!");
                    return await OnGetAsync(id);
                }

                var cartItems = JsonSerializer.Deserialize<List<CartItemDTO>>(CartDataJson);
                if (cartItems == null || !cartItems.Any()) return await OnGetAsync(id);

                decimal tongTienMonAn = cartItems.Sum(x => x.DonGia * x.SoLuong);
                decimal vat = tongTienMonAn * 0.1m;

                var hoaDon = new HOA_DON_NHA_HANG
                {
                    MaHoaDonNhaHang = "HD" + DateTime.Now.ToString("yyMMddHHmm"),
                    ThoiGianTao = DateTime.Now,
                    TongTienMonAn = tongTienMonAn,
                    GiamGia = 0,
                    ThanhTien = tongTienMonAn + vat,
                    HinhThucThanhToan = "Chưa thanh toán"
                };
                _context.HOA_DON_NHA_HANG.Add(hoaDon);

                foreach (var item in cartItems)
                {
                    var chiTiet = new CHI_TIET_DICH_VU
                    {
                        MaChiTietDichVu = Guid.NewGuid().ToString().Substring(0, 8),

                        // SỬA LỖI Ở DÒNG NÀY: Bỏ .ToString() đi, truyền thẳng số nguyên vào
                        SoLuong = item.SoLuong,

                        DonGia = item.DonGia,
                        ThanhTien = item.DonGia * item.SoLuong,
                        ThoiGianSuDung = DateTime.Now
                    };
                    _context.CHI_TIET_DICH_VU.Add(chiTiet);
                }

                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
                if (ban != null) ban.TrangThai = "Đang có khách";

                await _context.SaveChangesAsync();
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lưu Order: " + ex.Message);
                return await OnGetAsync(id);
            }
        }
    }
}