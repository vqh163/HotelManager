using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class SuaBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SuaBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ThemBanViewModel BanInfo { get; set; } = new ThemBanViewModel();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("./SoDoBan");

            var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
            if (ban == null) return RedirectToPage("./SoDoBan");

            BanInfo = new ThemBanViewModel
            {
                MaBan = ban.MaBan,
                TenBan = ban.TenBan ?? "",
                // ĐÃ FIX LỖI CS1503: Gán trực tiếp vì cả 2 đều là int?
                SucChua = ban.SucChua,
                TrangThai = ban.TrangThai ?? "Trống",
                TenKhachDat = ban.GetType().GetProperty("TenKhachDat")?.GetValue(ban)?.ToString()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == BanInfo.MaBan);
                if (ban == null) return Page();

                ban.TenBan = BanInfo.TenBan;
                ban.TrangThai = BanInfo.TrangThai;
                // ĐÃ FIX LỖI CS0029: Gán trực tiếp int?
                ban.SucChua = BanInfo.SucChua;

                if (ban.GetType().GetProperty("TenKhachDat") != null)
                {
                    ban.GetType().GetProperty("TenKhachDat")?.SetValue(ban, BanInfo.TrangThai == "Đã đặt trước" ? BanInfo.TenKhachDat : null);
                }

                if (BanInfo.TrangThai == "Đang có khách")
                {
                    var conn = _context.Database.GetDbConnection();
                    await conn.OpenAsync();
                    bool hasActiveInvoice = false;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(1) FROM HOA_DON_NHA_HANG WHERE HinhThucThanhToan = N'Đang phục vụ' AND (MaBan = @maBan OR BAN_NHA_HANGMaBan = @maBan)";
                        var param = cmd.CreateParameter();
                        param.ParameterName = "@maBan";
                        param.Value = ban.MaBan;
                        cmd.Parameters.Add(param);

                        try
                        {
                            hasActiveInvoice = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                        }
                        catch { }
                    }

                    if (!hasActiveInvoice)
                    {
                        string uniqueInvoiceId = "HD" + DateTime.Now.ToString("yyMMdd") + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                        var hoaDonMoi = new HOA_DON_NHA_HANG
                        {
                            MaHoaDonNhaHang = uniqueInvoiceId,
                            ThoiGianTao = DateTime.Now,
                            TongTienMonAn = 0,
                            GiamGia = 0,
                            ThanhTien = 0,
                            HinhThucThanhToan = "Đang phục vụ"
                        };

                        var dbProperties = hoaDonMoi.GetType().GetProperties();
                        foreach (var prop in dbProperties)
                        {
                            if (prop.Name == "MaBan" || prop.Name.EndsWith("MaBan"))
                            {
                                prop.SetValue(hoaDonMoi, ban.MaBan);
                                break;
                            }
                        }

                        _context.HOA_DON_NHA_HANG.Add(hoaDonMoi);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin bàn thành công!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi cập nhật: " + ex.Message);
                return Page();
            }
        }
    }
}