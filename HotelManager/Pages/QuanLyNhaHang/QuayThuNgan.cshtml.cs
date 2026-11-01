using HotelManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.ThuNgan
{
    // DTO chứa thông tin chi tiết từng món ăn (Giữ nguyên của bạn)
    public class ChiTietMonViewModel
    {
        public string TenMon { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }

    // DTO chứa dữ liệu tổng thể của thẻ Hóa đơn (Giữ nguyên của bạn)
    public class HoaDonViewModel
    {
        public string STT { get; set; } = string.Empty;
        public string MaHoaDonNhaHang { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public string HinhThucThanhToan { get; set; } = string.Empty;
        public string DanhSachMonJson { get; set; } = string.Empty;
        public decimal TamTinh { get; set; }
        public decimal PhiPhucVu { get; set; }
        public decimal ThueVAT { get; set; }
        public decimal TongCong { get; set; }
    }

    public class QuayThuNganModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuayThuNganModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<HoaDonViewModel> DanhSachHoaDon { get; set; } = new List<HoaDonViewModel>();

        public async Task OnGetAsync()
        {
            try
            {
                var dsHoaDonDb = await _context.HOA_DON_NHA_HANG.OrderByDescending(h => h.ThoiGianTao).ToListAsync();
                var dsBanDb = await _context.BAN_NHA_HANG.ToListAsync();

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    string maBanCuaHd = "";
                    var propMaBan = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");

                    if (propMaBan != null)
                        maBanCuaHd = propMaBan.GetValue(hd)?.ToString() ?? "";
                    else
                        try { maBanCuaHd = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }

                    var banTuongUng = dsBanDb.FirstOrDefault(b => b.MaBan == maBanCuaHd);
                    if (banTuongUng == null) continue;

                    string soBan = (banTuongUng.TenBan ?? "").Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                    if (string.IsNullOrEmpty(soBan)) soBan = "Trống";
                    else if (soBan.Length == 1 && char.IsDigit(soBan[0])) soBan = "0" + soBan;

                    string sttFormat = stt < 10 ? "0" + stt : stt.ToString();
                    stt++;

                    // --- BƯỚC ĐỒNG BỘ CSDL: ĐỌC VÀ CHUYỂN ĐỔI CHUẨN JSON TỪ GỌI MÓN ---
                    string jsonMonAn = "[]";
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null)
                            jsonMonAn = propJson.GetValue(hd)?.ToString() ?? "[]";
                        else
                            jsonMonAn = _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { }

                    var dsMonAnThuNgan = new List<ChiTietMonViewModel>();
                    try
                    {
                        if (!string.IsNullOrEmpty(jsonMonAn) && jsonMonAn != "[]")
                        {
                            using (var doc = JsonDocument.Parse(jsonMonAn))
                            {
                                foreach (var element in doc.RootElement.EnumerateArray())
                                {
                                    // Dò quét thông minh để khớp tên biến từ JSON cũ sang JSON mới của Quầy Thu Ngân
                                    string tenMon = "";
                                    if (element.TryGetProperty("tenDichVu", out var t1)) tenMon = t1.GetString() ?? "";
                                    else if (element.TryGetProperty("TenMon", out var t2)) tenMon = t2.GetString() ?? "";

                                    int soLuong = 1;
                                    if (element.TryGetProperty("soLuong", out var s1)) soLuong = s1.GetInt32();
                                    else if (element.TryGetProperty("SoLuong", out var s2)) soLuong = s2.GetInt32();

                                    decimal donGia = 0;
                                    if (element.TryGetProperty("donGia", out var d1)) donGia = d1.GetDecimal();
                                    else if (element.TryGetProperty("DonGia", out var d2)) donGia = d2.GetDecimal();

                                    string ghiChu = "";
                                    if (element.TryGetProperty("ghiChu", out var g1)) ghiChu = g1.GetString() ?? "";
                                    else if (element.TryGetProperty("GhiChu", out var g2)) ghiChu = g2.GetString() ?? "";

                                    dsMonAnThuNgan.Add(new ChiTietMonViewModel
                                    {
                                        TenMon = string.IsNullOrEmpty(tenMon) ? "Món ăn" : tenMon,
                                        SoLuong = soLuong,
                                        DonGia = donGia,
                                        ThanhTien = soLuong * donGia, // Tự động tính lại thành tiền
                                        GhiChu = ghiChu
                                    });
                                }
                            }
                        }
                    }
                    catch { }

                    // Mã hóa lại chuỗi JSON chuẩn để đẩy ra Giao diện HTML
                    string finalJson = Uri.EscapeDataString(JsonSerializer.Serialize(dsMonAnThuNgan));

                    // Tính toán lại tổng tiền từ danh sách đã đồng bộ
                    decimal tamTinh = dsMonAnThuNgan.Sum(x => x.ThanhTien);
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0;

                    decimal phiPhucVu = tamTinh * 0.05m;
                    decimal thueVAT = tamTinh * 0.10m;
                    decimal tongCong = tamTinh + phiPhucVu + thueVAT;
                    // -------------------------------------------------------------------

                    DanhSachHoaDon.Add(new HoaDonViewModel
                    {
                        STT = sttFormat,
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang ?? "",
                        TenBan = $"Bàn: {soBan}",
                        HinhThucThanhToan = hd.HinhThucThanhToan ?? "Chưa thanh toán",
                        DanhSachMonJson = finalJson,
                        TamTinh = tamTinh,
                        PhiPhucVu = phiPhucVu,
                        ThueVAT = thueVAT,
                        TongCong = tongCong
                    });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        public async Task<IActionResult> OnPostThanhToanAsync(string maHoaDonToPay)
        {
            try
            {
                if (string.IsNullOrEmpty(maHoaDonToPay)) return RedirectToPage();

                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonToPay);
                if (hd != null)
                {
                    hd.HinhThucThanhToan = "Đã Thanh Toán";
                    string maBanCuaHd = "";
                    var propMaBan = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");

                    if (propMaBan != null)
                        maBanCuaHd = propMaBan.GetValue(hd)?.ToString() ?? "";
                    else
                        try { maBanCuaHd = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }

                    var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBanCuaHd);
                    if (ban != null)
                    {
                        ban.TrangThai = "Đang dọn dẹp";
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Chốt hóa đơn {maHoaDonToPay} thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xử lý thanh toán: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}