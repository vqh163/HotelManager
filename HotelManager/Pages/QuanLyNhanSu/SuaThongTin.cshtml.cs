using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyNhanSu
{
    [Authorize(Policy = HangSoHeThong.PolicyNhanSu)]
    public class SuaThongTinModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuaThongTinModel> _logger;

        public SuaThongTinModel(ApplicationDbContext context, ILogger<SuaThongTinModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public NHAN_VIEN Input { get; set; } = new NHAN_VIEN();

        [BindProperty]
        public string MaChucVuSelected { get; set; } = string.Empty;

        [BindProperty]
        public decimal? LuongCoBanInput { get; set; }

        public SelectList DanhSachChucVuSelectList { get; set; } = default!;
        public string ThongBaoLoi { get; set; } = string.Empty;

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        public bool IsManager { get; set; } = false;

        private async Task LoadDropdownChucVuAsync()
        {
            var chucVus = await _context.CHUC_VU.AsNoTracking()
                .Where(cv => cv.TenChucVu != HangSoHeThong.VaiTroKhachHang)
                .OrderBy(cv => cv.MaChucVu)
                .ToListAsync();

            DanhSachChucVuSelectList = new SelectList(chucVus, "MaChucVu", "TenChucVu", MaChucVuSelected);
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            IsManager = User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);

            try
            {
                var nhanVien = await _context.NHAN_VIEN.AsNoTracking().FirstOrDefaultAsync(n => n.MaNV == id);
                if (nhanVien == null) return NotFound();

                Input = nhanVien;
                MaChucVuSelected = nhanVien.CHUC_VUMaChucVu ?? string.Empty;
                LuongCoBanInput = nhanVien.LuongCoBan;

                await LoadDropdownChucVuAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải thông tin nhân viên {Id}", id);
                ThongBaoLoi = "Đã xảy ra lỗi khi tải thông tin nhân viên.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var nhanVienDb = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == Input.MaNV);
                if (nhanVienDb == null) return NotFound();

                string cccd = (Input.SoCCCD ?? string.Empty).Trim();
                string sdt = (Input.SDT ?? string.Empty).Trim();
                string email = (Input.Email ?? string.Empty).Trim();

                // Kiểm tra trùng CCCD / SĐT / Email với nhân viên khác
                bool trungThongTin = await _context.NHAN_VIEN.AsNoTracking()
                    .AnyAsync(n => n.MaNV != nhanVienDb.MaNV &&
                        ((!string.IsNullOrEmpty(cccd) && n.SoCCCD == cccd) ||
                         (!string.IsNullOrEmpty(sdt) && n.SDT == sdt) ||
                         (!string.IsNullOrEmpty(email) && n.Email == email)));
                if (trungThongTin)
                {
                    ThongBaoLoi = "Số CCCD, SĐT hoặc Email đã tồn tại ở một nhân viên khác!";
                    await LoadDropdownChucVuAsync();
                    return Page();
                }

                if (LuongCoBanInput.HasValue && LuongCoBanInput.Value < 0)
                {
                    ThongBaoLoi = "Mức lương cơ bản không được là số âm!";
                    await LoadDropdownChucVuAsync();
                    return Page();
                }

                // Cập nhật đầy đủ các trường bao gồm CHUC_VUMaChucVu và LuongCoBan
                nhanVienDb.HoTen = Input.HoTen?.Trim();
                nhanVienDb.SoCCCD = cccd;
                nhanVienDb.GioiTinh = Input.GioiTinh;
                nhanVienDb.NgaySinh = Input.NgaySinh;
                nhanVienDb.SDT = sdt;
                nhanVienDb.Email = email;
                nhanVienDb.NgayVaoLam = Input.NgayVaoLam;
                nhanVienDb.CHUC_VUMaChucVu = MaChucVuSelected;
                if (LuongCoBanInput.HasValue) nhanVienDb.LuongCoBan = LuongCoBanInput.Value;

                // Tự động đồng bộ VaiTro trên tài khoản TAI_KHOAN (nếu nhân viên đã được cấp tài khoản)
                if (!string.IsNullOrWhiteSpace(MaChucVuSelected))
                {
                    var chucVuDb = await _context.CHUC_VU.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.MaChucVu == MaChucVuSelected);
                    if (chucVuDb != null && !string.IsNullOrWhiteSpace(chucVuDb.TenChucVu))
                    {
                        var tkLienKet = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.NHAN_VIENMaNV == nhanVienDb.MaNV);
                        if (tkLienKet != null)
                        {
                            tkLienKet.VaiTro = chucVuDb.TenChucVu.Trim();
                            _context.TAI_KHOAN.Update(tkLienKet);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Cập nhật thành công thông tin nhân viên: {nhanVienDb.HoTen}";
                return RedirectToPage("./QuanLyDanhSachNhanVien");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật thông tin nhân viên {MaNV}", Input.MaNV);
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi lưu cập nhật thông tin nhân viên.";
                await LoadDropdownChucVuAsync();
                return Page();
            }
        }
    }
}
