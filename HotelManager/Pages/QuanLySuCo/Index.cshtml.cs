using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySuCo
{
    // Phân quyền đúng 4 vai trò tham gia UC_07 theo đặc tả mục 2.1.1.7 (+ Giám đốc/Quản trị viên)
    [Authorize(Roles = "Quản lý,Giám đốc,Quản trị viên,Lễ tân,Nhân viên Buồng phòng,Buồng phòng,Nhân viên Kỹ thuật,Kỹ thuật")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Thông tin 1 dòng vật tư đã xuất kho lưu trong DanhSachVatTuJson
        public class VatTuTieuHaoDto
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien { get; set; }
            public string ThoiGianXuat { get; set; } = string.Empty;
        }

        // Class hứng danh sách nhiều vật tư Kỹ thuật chọn thêm trên Form
        public class VatTuChonInputModel
        {
            public string MaVatTu { get; set; } = string.Empty;
            public int SoLuong { get; set; }
        }

        // DTO vật tư kho đẩy sang JavaScript để tự hiển thị đơn vị tính & tồn kho
        public class KhoVatTuOptionDto
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuongTon { get; set; }
            public decimal DonGiaNhap { get; set; }
        }

        // ViewModel tổng hợp cho từng Phiếu sự cố hiển thị trên Dashboard
        public class SuCoViewModel
        {
            public string MaPhieuBaoCao { get; set; } = string.Empty;
            public string? MaPhong { get; set; }
            public string ViTriSuCo { get; set; } = string.Empty;
            public bool LaKhuVucChung { get; set; }
            public string MoTaSuCo { get; set; } = string.Empty;
            public string MucDoUuTien { get; set; } = "Trung bình";
            public string TrangThai { get; set; } = "Chờ tiếp nhận";
            public string NguoiTao { get; set; } = string.Empty;
            public string NguoiXuLy { get; set; } = "Chưa có người nhận";
            public string? MaNVKyThuat { get; set; }
            public string AnhTruocSua { get; set; } = string.Empty;
            public string AnhSauSua { get; set; } = string.Empty;
            public string GhiChuXuLy { get; set; } = string.Empty;
            public string LyDoTuChoi { get; set; } = string.Empty;
            public List<VatTuTieuHaoDto> DanhSachVatTu { get; set; } = new List<VatTuTieuHaoDto>();
            public decimal TienVatTu { get; set; }
            public decimal ChiPhiPhatSinh { get; set; }
            public decimal TongChiPhi { get; set; }
            public bool DoKhachLamHong { get; set; }
            public bool CoKhoaPhong { get; set; }
            public string ThoiGianTaoStr { get; set; } = string.Empty;
            public string ThoiGianTiepNhanStr { get; set; } = string.Empty;
            public string ThoiGianHoanThanhStr { get; set; } = string.Empty;
        }

        // === BỘ LỌC TÌM KIẾM (GET) ===
        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UuTienLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? LoaiViTriLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        // === THUỘC TÍNH BINDING FORM TẠO PHIẾU SỰ CỐ MỚI ===
        [BindProperty]
        public string LoaiViTriMoi { get; set; } = "Phong";

        [BindProperty]
        public string? MaPhongMoi { get; set; }

        [BindProperty]
        public string? KhuVucChungMoi { get; set; }

        [BindProperty]
        public string MucDoUuTienMoi { get; set; } = "Trung bình";

        [BindProperty]
        public string MoTaSuCoMoi { get; set; } = string.Empty;

        [BindProperty]
        public bool KhoaPhongDeSuaMoi { get; set; } = false;

        [BindProperty]
        public bool DoKhachLamHongMoi { get; set; } = false;

        [BindProperty]
        public IFormFile? AnhHienTruongMoi { get; set; }

        // === THUỘC TÍNH BINDING KỸ THUẬT TIẾP NHẬN, CẬP NHẬT & XUẤT VẬT TƯ ===
        [BindProperty]
        public string MaPhieuThaoTac { get; set; } = string.Empty;

        [BindProperty]
        public string HanhDongKyThuat { get; set; } = "LuuTam";

        [BindProperty]
        public string? GhiChuXuLyKT { get; set; }

        [BindProperty]
        public decimal ChiPhiPhatSinhKT { get; set; } = 0;

        [BindProperty]
        public bool DoKhachLamHongKT { get; set; } = false;

        [BindProperty]
        public bool CongPhiVaoHoaDonPhongKT { get; set; } = false;

        [BindProperty]
        public IFormFile? AnhSauSuaChuaKT { get; set; }

        [BindProperty]
        public List<VatTuChonInputModel> VatTuXuatThem { get; set; } = new List<VatTuChonInputModel>();

        // === THUỘC TÍNH BINDING QUẢN LÝ NGHIỆM THU ===
        [BindProperty]
        public string KetQuaNghiemThu { get; set; } = "Dat";

        [BindProperty]
        public string? LyDoTuChoiQL { get; set; }

        // === DỮ LIỆU ĐẨY RA VIEW ===
        public List<SuCoViewModel> DanhSachSuCo { get; set; } = new List<SuCoViewModel>();
        public List<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();
        public List<KhoVatTuOptionDto> DanhSachVatTuKho { get; set; } = new List<KhoVatTuOptionDto>();

        public string DanhSachSuCoJson { get; set; } = "[]";
        public string DanhSachVatTuKhoJson { get; set; } = "[]";

        public int TongSoPhieu { get; set; }
        public int SoChoTiepNhan { get; set; }
        public int SoDangSuaChua { get; set; }
        public int SoChoNghiemThu { get; set; }
        public int SoHoanThanh { get; set; }

        // Cờ phân quyền theo vai trò
        public bool IsKyThuat => User.IsInRole("Nhân viên Kỹ thuật") || User.IsInRole("Kỹ thuật");
        public bool IsQuanLy => User.IsInRole("Quản lý") || User.IsInRole("Giám đốc") || User.IsInRole("Quản trị viên");
        public bool CanCreateTicket => IsKyThuat || IsQuanLy || User.IsInRole("Lễ tân") || User.IsInRole("Nhân viên Buồng phòng") || User.IsInRole("Buồng phòng");

        // Lấy thông tin (MaNV, HoTen) của người đang đăng nhập từ Cookie
        private async Task<(string? MaNV, string HoTen)> LayThongTinNguoiDungHienTaiAsync()
        {
            string username = User.Identity?.Name ?? "Hệ thống";
            var tk = await _context.TAI_KHOAN.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (tk != null && !string.IsNullOrWhiteSpace(tk.NHAN_VIENMaNV))
            {
                var nv = await _context.NHAN_VIEN.AsNoTracking()
                    .FirstOrDefaultAsync(n => n.MaNV == tk.NHAN_VIENMaNV);
                return (tk.NHAN_VIENMaNV, nv?.HoTen ?? username);
            }

            return (null, username);
        }

        // Lưu hình ảnh hiện trường (Trước sửa / Sau sửa) an toàn qua BaoMatHelper
        private async Task<string> SaveIncidentImageAsync(IFormFile? file, string prefix)
        {
            if (file == null || file.Length == 0) return string.Empty;

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "suco");
            var (ok, ketQua) = await HotelManager.Helpers.BaoMatHelper.LuuAnhAnToanAsync(file, uploadFolder, prefix);
            if (!ok)
            {
                throw new InvalidOperationException(ketQua);
            }

            return $"/uploads/suco/{ketQua}";
        }

        // Đảm bảo mỗi phòng có 1 bản ghi TRANG_THAI_PHONG độc lập (đồng bộ với SoDoPhong)
        private async Task<TRANG_THAI_PHONG> EnsureRoomStatusRecordAsync(PHONG phong)
        {
            string maTTRieng = "TTP_" + (phong.SoPhong ?? phong.MaPhong).Trim();
            if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);

            var ttRieng = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == maTTRieng);
            if (ttRieng == null)
            {
                TRANG_THAI_PHONG? ttCu = null;
                if (!string.IsNullOrWhiteSpace(phong.MaTrangThai))
                {
                    ttCu = await _context.TRANG_THAI_PHONG.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                }

                ttRieng = new TRANG_THAI_PHONG
                {
                    MaTrangThai = maTTRieng,
                    TrangThaiPhong = ttCu?.TrangThaiPhong ?? "Trống",
                    TrangThaiVeSinh = ttCu?.TrangThaiVeSinh ?? "Sạch",
                    ThoiGianDonVeSinh = ttCu?.ThoiGianDonVeSinh ?? DateTime.Now
                };
                _context.TRANG_THAI_PHONG.Add(ttRieng);
            }

            if (phong.MaTrangThai != maTTRieng)
            {
                phong.MaTrangThai = maTTRieng;
                _context.PHONG.Update(phong);
            }

            return ttRieng;
        }

        // Ghi thông báo liên phòng ban vào NHAT_KY_HE_THONG
        private void ThemThongBaoHeThong(string hanhDong, string chiTiet)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            string maLog = $"LG{DateTime.Now:yyMMddHHmmss}{suffix}";
            _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
            {
                MaNhatKy = maLog.Length > 20 ? maLog.Substring(0, 20) : maLog,
                TenDangNhap = User.Identity?.Name ?? "Hệ thống",
                HanhDong = hanhDong,
                ChiTiet = chiTiet.Length > 255 ? chiTiet.Substring(0, 255) : chiTiet
            });
        }

        // 1. ONGET: Tải danh sách sự cố, phòng và vật tư kho bằng LINQ
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DanhSachPhong = await _context.PHONG.AsNoTracking()
                    .OrderBy(p => p.SoPhong)
                    .ToListAsync();

                var khoList = await _context.KHO_VAT_TU.AsNoTracking()
                    .OrderBy(v => v.TenVatTu)
                    .ToListAsync();

                DanhSachVatTuKho = khoList.Select(v => new KhoVatTuOptionDto
                {
                    MaVatTu = v.MaVatTu,
                    TenVatTu = v.TenVatTu ?? v.MaVatTu,
                    LoaiVatTu = v.LoaiVatTu ?? "Vật tư",
                    DonViTinh = v.DonViTinh ?? "Cái",
                    SoLuongTon = Convert.ToInt32((object?)v.SoLuongTon ?? 0),
                    DonGiaNhap = Convert.ToDecimal((object?)v.DonGiaNhap ?? 0)
                }).ToList();

                DanhSachVatTuKhoJson = JsonSerializer.Serialize(DanhSachVatTuKho);

                var nvDict = await _context.NHAN_VIEN.AsNoTracking()
                    .Where(n => n.MaNV != null)
                    .ToDictionaryAsync(n => n.MaNV, n => n.HoTen ?? n.MaNV);

                var phongDict = DanhSachPhong.ToDictionary(p => p.MaPhong, p => p.SoPhong ?? p.MaPhong);

                var ticketsDb = await _context.PHIEU_BAO_CAO_SU_CO.AsNoTracking()
                    .OrderByDescending(p => p.ThoiGianTao)
                    .ToListAsync();

                var allViewModels = new List<SuCoViewModel>();

                foreach (var t in ticketsDb)
                {
                    string trangThai = string.IsNullOrWhiteSpace(t.TrangThai) ? "Chờ tiếp nhận" : t.TrangThai.Trim();
                    string viTri = t.ViTriSuCo ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(viTri))
                    {
                        if (!string.IsNullOrWhiteSpace(t.MaPhong) && phongDict.TryGetValue(t.MaPhong, out var sp))
                        {
                            viTri = $"Phòng {sp}";
                        }
                        else if (!string.IsNullOrWhiteSpace(t.MoTaSuCo) && t.MoTaSuCo.StartsWith("[") && t.MoTaSuCo.Contains("]"))
                        {
                            int endIdx = t.MoTaSuCo.IndexOf("]");
                            viTri = t.MoTaSuCo.Substring(1, endIdx - 1);
                        }
                        else
                        {
                            viTri = "Khu vực chung";
                        }
                    }

                    string nguoiTao = !string.IsNullOrWhiteSpace(t.NguoiTaoTen)
                        ? t.NguoiTaoTen
                        : (!string.IsNullOrWhiteSpace(t.NHAN_VIENMaNV) && nvDict.ContainsKey(t.NHAN_VIENMaNV)
                            ? nvDict[t.NHAN_VIENMaNV]
                            : (t.NHAN_VIENMaNV ?? "Nhân viên"));

                    string nguoiXuLy = !string.IsNullOrWhiteSpace(t.TenNVKyThuat)
                        ? t.TenNVKyThuat
                        : (!string.IsNullOrWhiteSpace(t.MaNVKyThuat) && nvDict.ContainsKey(t.MaNVKyThuat)
                            ? nvDict[t.MaNVKyThuat]
                            : "Chưa có người nhận");

                    var dsVatTu = new List<VatTuTieuHaoDto>();
                    if (!string.IsNullOrWhiteSpace(t.DanhSachVatTuJson))
                    {
                        try
                        {
                            dsVatTu = JsonSerializer.Deserialize<List<VatTuTieuHaoDto>>(t.DanhSachVatTuJson) ?? new List<VatTuTieuHaoDto>();
                        }
                        catch { dsVatTu = new List<VatTuTieuHaoDto>(); }
                    }

                    decimal tienVatTu = dsVatTu.Sum(x => x.ThanhTien);
                    decimal chiPhiPhatSinh = t.ChiPhiPhatSinh ?? 0;
                    decimal tongChiPhi = t.TongChiPhi ?? (tienVatTu + chiPhiPhatSinh);

                    allViewModels.Add(new SuCoViewModel
                    {
                        MaPhieuBaoCao = t.MaPhieuBaoCao,
                        MaPhong = t.MaPhong,
                        ViTriSuCo = viTri,
                        LaKhuVucChung = string.IsNullOrWhiteSpace(t.MaPhong),
                        MoTaSuCo = t.MoTaSuCo ?? "",
                        MucDoUuTien = string.IsNullOrWhiteSpace(t.MucDoUuTien) ? "Trung bình" : t.MucDoUuTien,
                        TrangThai = trangThai,
                        NguoiTao = nguoiTao,
                        NguoiXuLy = nguoiXuLy,
                        MaNVKyThuat = t.MaNVKyThuat,
                        AnhTruocSua = t.HinhAnhDinhKem ?? "",
                        AnhSauSua = t.AnhSauSuaChua ?? "",
                        GhiChuXuLy = t.GhiChuXuLy ?? "",
                        LyDoTuChoi = t.LyDoTuChoi ?? "",
                        DanhSachVatTu = dsVatTu,
                        TienVatTu = tienVatTu,
                        ChiPhiPhatSinh = chiPhiPhatSinh,
                        TongChiPhi = tongChiPhi,
                        DoKhachLamHong = t.DoKhachLamHong,
                        CoKhoaPhong = t.CoKhoaPhong,
                        ThoiGianTaoStr = t.ThoiGianTao.HasValue ? t.ThoiGianTao.Value.ToString("dd/MM/yyyy HH:mm") : "",
                        ThoiGianTiepNhanStr = t.ThoiGianTiepNhan.HasValue ? t.ThoiGianTiepNhan.Value.ToString("dd/MM/yyyy HH:mm") : "",
                        ThoiGianHoanThanhStr = t.ThoiGianHoanThanh.HasValue ? t.ThoiGianHoanThanh.Value.ToString("dd/MM/yyyy HH:mm") : ""
                    });
                }

                // Tính các chỉ số KPI
                TongSoPhieu = allViewModels.Count;
                SoChoTiepNhan = allViewModels.Count(x => x.TrangThai == "Chờ tiếp nhận");
                SoDangSuaChua = allViewModels.Count(x => x.TrangThai == "Đang sửa chữa");
                SoChoNghiemThu = allViewModels.Count(x => x.TrangThai == "Chờ nghiệm thu");
                SoHoanThanh = allViewModels.Count(x => x.TrangThai == "Hoàn thành");

                // Lọc theo điều kiện người dùng chọn
                var filtered = allViewModels.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(TrangThaiLoc) && TrangThaiLoc != "TatCa")
                    filtered = filtered.Where(x => x.TrangThai == TrangThaiLoc);

                if (!string.IsNullOrWhiteSpace(UuTienLoc) && UuTienLoc != "TatCa")
                    filtered = filtered.Where(x => x.MucDoUuTien == UuTienLoc);

                if (!string.IsNullOrWhiteSpace(LoaiViTriLoc) && LoaiViTriLoc != "TatCa")
                {
                    if (LoaiViTriLoc == "Phong") filtered = filtered.Where(x => !x.LaKhuVucChung);
                    else if (LoaiViTriLoc == "KhuVucChung") filtered = filtered.Where(x => x.LaKhuVucChung);
                }

                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    string kw = TuKhoa.Trim().ToLower();
                    filtered = filtered.Where(x =>
                        x.MaPhieuBaoCao.ToLower().Contains(kw) ||
                        x.ViTriSuCo.ToLower().Contains(kw) ||
                        x.MoTaSuCo.ToLower().Contains(kw) ||
                        x.NguoiTao.ToLower().Contains(kw) ||
                        x.NguoiXuLy.ToLower().Contains(kw));
                }

                DanhSachSuCo = filtered
                    .OrderBy(x => x.TrangThai == "Hoàn thành" ? 1 : 0)
                    .ThenByDescending(x => x.MucDoUuTien == "Khẩn cấp")
                    .ThenByDescending(x => x.MucDoUuTien == "Cao")
                    .ToList();

                DanhSachSuCoJson = JsonSerializer.Serialize(DanhSachSuCo);
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách sự cố kỹ thuật: " + ex.Message;
                return Page();
            }
        }

        // 2. BƯỚC 1: Tạo phiếu báo cáo sự cố mới
        public async Task<IActionResult> OnPostTaoPhieuSuCoAsync()
        {
            if (!CanCreateTicket)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo phiếu báo cáo sự cố!";
                return RedirectToPage();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(MoTaSuCoMoi))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập mô tả chi tiết sự cố!";
                    return RedirectToPage();
                }

                string? maPhongChot = null;
                string viTriChot = string.Empty;
                PHONG? phongChon = null;

                if (LoaiViTriMoi == "Phong")
                {
                    if (string.IsNullOrWhiteSpace(MaPhongMoi))
                    {
                        TempData["ErrorMessage"] = "Vui lòng chọn phòng xảy ra sự cố!";
                        return RedirectToPage();
                    }

                    phongChon = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == MaPhongMoi);
                    if (phongChon == null)
                    {
                        TempData["ErrorMessage"] = "Phòng được chọn không tồn tại trong hệ thống!";
                        return RedirectToPage();
                    }

                    maPhongChot = phongChon.MaPhong;
                    viTriChot = $"Phòng {phongChon.SoPhong}";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(KhuVucChungMoi))
                    {
                        TempData["ErrorMessage"] = "Vui lòng chọn hoặc nhập tên khu vực chung xảy ra sự cố!";
                        return RedirectToPage();
                    }
                    viTriChot = KhuVucChungMoi.Trim();
                }

                DateTime now = DateTime.Now;
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                var (maNVTao, tenNguoiTao) = await LayThongTinNguoiDungHienTaiAsync();

                string anhHienTruongUrl = await SaveIncidentImageAsync(AnhHienTruongMoi, "BEFORE");

                bool canKhoaPhong = phongChon != null &&
                                    (KhoaPhongDeSuaMoi || MucDoUuTienMoi == "Khẩn cấp" || MucDoUuTienMoi == "Cao");

                string ghiChuKhoaPhongMsg = "";
                bool daThucHienKhoa = false;

                if (canKhoaPhong && phongChon != null)
                {
                    bool phongDangCoKhach = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                   join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                   where ct.PHONGMaPhong == phongChon.MaPhong && dp.TrangThaiDatPhong == "DANG_O"
                                                   select dp.MaDatPhong).AnyAsync();

                    var trangThaiPhong = await EnsureRoomStatusRecordAsync(phongChon);
                    trangThaiPhong.TrangThaiPhong = "Bảo trì";
                    _context.TRANG_THAI_PHONG.Update(trangThaiPhong);
                    daThucHienKhoa = true;

                    ghiChuKhoaPhongMsg = phongDangCoKhach
                        ? $" (Lưu ý: {viTriChot} hiện đang có khách lưu trú, Lễ tân cần thực hiện nghiệp vụ Đổi phòng cho khách trên Sơ đồ phòng!)"
                        : $" Đã tự động chuyển {viTriChot} sang trạng thái 'Bảo trì' trên Sơ đồ phòng.";
                }

                var phieuMoi = new PHIEU_BAO_CAO_SU_CO
                {
                    MaPhieuBaoCao = $"SC{now:yyMMddHHmmss}{suffix}",
                    NHAN_VIENMaNV = maNVTao,
                    NguoiTaoTen = tenNguoiTao,
                    MaPhong = maPhongChot,
                    ViTriSuCo = viTriChot,
                    MoTaSuCo = MoTaSuCoMoi.Trim(),
                    MucDoUuTien = MucDoUuTienMoi,
                    HinhAnhDinhKem = anhHienTruongUrl,
                    TrangThai = "Chờ tiếp nhận",
                    DoKhachLamHong = DoKhachLamHongMoi,
                    CoKhoaPhong = daThucHienKhoa,
                    ChiPhiPhatSinh = 0,
                    TongChiPhi = 0,
                    ThoiGianTao = now
                };

                _context.PHIEU_BAO_CAO_SU_CO.Add(phieuMoi);

                ThemThongBaoHeThong(
                    "Yêu cầu Kỹ thuật",
                    $"[Nhân viên Kỹ thuật] Phiếu sự cố mới {phieuMoi.MaPhieuBaoCao} tại {viTriChot} (Mức độ: {MucDoUuTienMoi}): {MoTaSuCoMoi.Trim()}"
                );

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo Phiếu sự cố {phieuMoi.MaPhieuBaoCao} tại {viTriChot} thành công!{ghiChuKhoaPhongMsg}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tạo phiếu sự cố: " + ex.Message;
            }

            return RedirectToPage();
        }

        // 3. BƯỚC 2: Nhân viên Kỹ thuật tiếp nhận phiếu sửa chữa
        public async Task<IActionResult> OnPostTiepNhanPhieuAsync()
        {
            if (!IsKyThuat)
            {
                TempData["ErrorMessage"] = "Chỉ Nhân viên Kỹ thuật mới có quyền tiếp nhận xử lý phiếu sự cố!";
                return RedirectToPage();
            }

            try
            {
                var phieu = await _context.PHIEU_BAO_CAO_SU_CO
                    .FirstOrDefaultAsync(p => p.MaPhieuBaoCao == MaPhieuThaoTac);

                if (phieu == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu sự cố cần tiếp nhận!";
                    return RedirectToPage();
                }

                string trangThaiHienTai = string.IsNullOrWhiteSpace(phieu.TrangThai) ? "Chờ tiếp nhận" : phieu.TrangThai.Trim();
                if (trangThaiHienTai != "Chờ tiếp nhận")
                {
                    TempData["ErrorMessage"] = $"Phiếu {phieu.MaPhieuBaoCao} hiện đang ở trạng thái '{trangThaiHienTai}', không thể tiếp nhận lại!";
                    return RedirectToPage();
                }

                var (maNVKT, tenNVKT) = await LayThongTinNguoiDungHienTaiAsync();

                phieu.TrangThai = "Đang sửa chữa";
                phieu.MaNVKyThuat = maNVKT;
                phieu.TenNVKyThuat = tenNVKT;
                phieu.ThoiGianTiepNhan = DateTime.Now;
                _context.PHIEU_BAO_CAO_SU_CO.Update(phieu);

                ThemThongBaoHeThong(
                    "Thông báo Kỹ thuật",
                    $"[Quản lý] Kỹ thuật viên {tenNVKT} đã tiếp nhận sửa chữa phiếu {phieu.MaPhieuBaoCao} tại {phieu.ViTriSuCo ?? "Khách sạn"}."
                );

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Bạn đã tiếp nhận phiếu {phieu.MaPhieuBaoCao}. Trạng thái đã chuyển sang 'Đang sửa chữa'!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tiếp nhận phiếu: " + ex.Message;
            }

            return RedirectToPage();
        }

        // 4. BƯỚC 3: Kỹ thuật cập nhật tiến độ, xuất nhiều vật tư (trừ kho) & gửi nghiệm thu
        public async Task<IActionResult> OnPostCapNhatSuaChuaAsync()
        {
            if (!IsKyThuat)
            {
                TempData["ErrorMessage"] = "Chỉ Nhân viên Kỹ thuật mới được cập nhật tiến độ sửa chữa và xuất vật tư!";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _context.PHIEU_BAO_CAO_SU_CO
                    .FirstOrDefaultAsync(p => p.MaPhieuBaoCao == MaPhieuThaoTac);

                if (phieu == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu sự cố!";
                    return RedirectToPage();
                }

                if (phieu.TrangThai != "Đang sửa chữa")
                {
                    TempData["ErrorMessage"] = $"Chỉ được cập nhật khi phiếu đang ở trạng thái 'Đang sửa chữa' (Hiện tại: {phieu.TrangThai}).";
                    return RedirectToPage();
                }

                // Kiểm tra đúng kỹ thuật viên đã tiếp nhận phiếu (hoặc Quản lý) mới được cập nhật
                var (maNVHienTai, _) = await LayThongTinNguoiDungHienTaiAsync();
                if (!IsQuanLy && !string.IsNullOrWhiteSpace(phieu.MaNVKyThuat) && !string.IsNullOrWhiteSpace(maNVHienTai) && phieu.MaNVKyThuat != maNVHienTai)
                {
                    TempData["ErrorMessage"] = "Phiếu sự cố này đang do nhân viên kỹ thuật khác phụ trách xử lý!";
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;

                // Đọc danh sách vật tư đã xuất trước đó để không trừ kho lặp lại
                var dsVatTuDaXuat = new List<VatTuTieuHaoDto>();
                if (!string.IsNullOrWhiteSpace(phieu.DanhSachVatTuJson))
                {
                    try
                    {
                        dsVatTuDaXuat = JsonSerializer.Deserialize<List<VatTuTieuHaoDto>>(phieu.DanhSachVatTuJson) ?? new List<VatTuTieuHaoDto>();
                    }
                    catch { dsVatTuDaXuat = new List<VatTuTieuHaoDto>(); }
                }

                // Kiểm tra tồn kho & trừ trực tiếp KHO_VAT_TU cho danh sách vật tư mới xuất
                var dsMoiHopLe = VatTuXuatThem?
                    .Where(x => !string.IsNullOrWhiteSpace(x.MaVatTu) && x.SoLuong > 0)
                    .GroupBy(x => x.MaVatTu)
                    .Select(g => new { MaVatTu = g.Key, SoLuong = g.Sum(i => i.SoLuong) })
                    .ToList();

                var canhBaoTonThap = new List<string>();

                if (dsMoiHopLe != null && dsMoiHopLe.Any())
                {
                    var maVatTuList = dsMoiHopLe.Select(x => x.MaVatTu).ToList();
                    var khoEntities = await _context.KHO_VAT_TU
                        .Where(v => maVatTuList.Contains(v.MaVatTu))
                        .ToListAsync();

                    foreach (var item in dsMoiHopLe)
                    {
                        var vatTuDb = khoEntities.FirstOrDefault(v => v.MaVatTu == item.MaVatTu);
                        if (vatTuDb == null)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Vật tư mã '{item.MaVatTu}' không tồn tại trong kho!";
                            return RedirectToPage();
                        }

                        int tonHienTai = Convert.ToInt32((object?)vatTuDb.SoLuongTon ?? 0);
                        int tonToiThieu = Convert.ToInt32((object?)vatTuDb.SoLuongTonToiThieu ?? 0);
                        decimal donGiaVatTu = Convert.ToDecimal((object?)vatTuDb.DonGiaNhap ?? 0);

                        if (item.SoLuong > tonHienTai)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Không đủ tồn kho cho vật tư '{vatTuDb.TenVatTu}'! Yêu cầu xuất {item.SoLuong} {vatTuDb.DonViTinh}, nhưng tồn kho hiện chỉ còn {tonHienTai} {vatTuDb.DonViTinh}.";
                            return RedirectToPage();
                        }

                        // Trừ kho nguyên tử chống âm kho khi đa luồng
                        int affected = await _context.KHO_VAT_TU
                            .Where(v => v.MaVatTu == vatTuDb.MaVatTu && (v.SoLuongTon ?? 0) >= item.SoLuong)
                            .ExecuteUpdateAsync(s => s.SetProperty(v => v.SoLuongTon, v => (v.SoLuongTon ?? 0) - item.SoLuong));

                        if (affected == 0)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Không đủ tồn kho cho vật tư '{vatTuDb.TenVatTu}'!";
                            return RedirectToPage();
                        }

                        int tonSauKhiTru = tonHienTai - item.SoLuong;
                        vatTuDb.SoLuongTon = tonSauKhiTru;

                        // Ghi đồng bộ phiếu xuất kho vào bảng PHIEU_NHAP_XUAT_KHO
                        _context.PHIEU_NHAP_XUAT_KHO.Add(new PHIEU_NHAP_XUAT_KHO
                        {
                            MaPhieu = HotelManager.Helpers.BaoMatHelper.SinhMaNgauNhien("PX"),
                            LoaiGiaoDich = "XUAT",
                            KHO_VAT_TUMaVatTu = vatTuDb.MaVatTu,
                            TenVatTu = vatTuDb.TenVatTu ?? vatTuDb.MaVatTu,
                            LoaiKho = vatTuDb.LoaiVatTu ?? "Kho kỹ thuật",
                            DonViTinh = vatTuDb.DonViTinh ?? "Cái",
                            SoLuong = item.SoLuong,
                            DonGia = donGiaVatTu,
                            DoiTacHoacBoPhan = "Bộ phận Kỹ thuật",
                            GhiChu = $"Xuất vật tư sửa chữa phiếu sự cố {phieu.MaPhieuBaoCao} ({phieu.ViTriSuCo})",
                            NguoiThucHien = User.Identity?.Name,
                            MaThamChieu = phieu.MaPhieuBaoCao,
                            ThoiGian = now
                        });

                        dsVatTuDaXuat.Add(new VatTuTieuHaoDto
                        {
                            MaVatTu = vatTuDb.MaVatTu,
                            TenVatTu = vatTuDb.TenVatTu ?? vatTuDb.MaVatTu,
                            DonViTinh = vatTuDb.DonViTinh ?? "Cái",
                            SoLuong = item.SoLuong,
                            DonGia = donGiaVatTu,
                            ThanhTien = donGiaVatTu * item.SoLuong,
                            ThoiGianXuat = now.ToString("dd/MM/yyyy HH:mm")
                        });

                        if (string.IsNullOrWhiteSpace(phieu.KHO_VAT_TUMaVatTu))
                        {
                            phieu.KHO_VAT_TUMaVatTu = vatTuDb.MaVatTu;
                        }

                        if (tonSauKhiTru <= tonToiThieu)
                        {
                            canhBaoTonThap.Add($"{vatTuDb.TenVatTu} (còn {tonSauKhiTru} {vatTuDb.DonViTinh})");
                            ThemThongBaoHeThong(
                                "Cảnh báo Tồn kho",
                                $"[Thủ kho] Vật tư '{vatTuDb.TenVatTu}' vừa xuất cho sửa chữa phiếu {phieu.MaPhieuBaoCao}, tồn kho chỉ còn {tonSauKhiTru} {vatTuDb.DonViTinh} (<= tối thiểu {tonToiThieu}). Cần đề xuất nhập hàng!"
                            );
                        }
                    }
                }

                // Lưu ảnh sau sửa chữa nếu có tải lên
                if (AnhSauSuaChuaKT != null && AnhSauSuaChuaKT.Length > 0)
                {
                    phieu.AnhSauSuaChua = await SaveIncidentImageAsync(AnhSauSuaChuaKT, "AFTER");
                }

                if (HanhDongKyThuat == "GuiNghiemThu" && string.IsNullOrWhiteSpace(phieu.AnhSauSuaChua))
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Vui lòng chụp hoặc tải lên Hình ảnh sau khi sửa chữa xong trước khi gửi Quản lý nghiệm thu!";
                    return RedirectToPage();
                }

                decimal tongTienVatTu = dsVatTuDaXuat.Sum(x => x.ThanhTien);
                decimal chiPhiPhatSinh = ChiPhiPhatSinhKT < 0 ? 0 : ChiPhiPhatSinhKT;

                phieu.DanhSachVatTuJson = JsonSerializer.Serialize(dsVatTuDaXuat);
                phieu.ChiPhiPhatSinh = chiPhiPhatSinh;
                phieu.TongChiPhi = tongTienVatTu + chiPhiPhatSinh;
                phieu.DoKhachLamHong = DoKhachLamHongKT;

                if (!string.IsNullOrWhiteSpace(GhiChuXuLyKT))
                {
                    phieu.GhiChuXuLy = GhiChuXuLyKT.Trim();
                }

                // Cộng phí đền bù vào hóa đơn phòng theo cơ chế UPSERT (1 mã duy nhất theo phiếu sự cố, chống cộng lặp)
                string maChiTietDenBu = ("DB" + (phieu.MaPhieuBaoCao.Length > 2 ? phieu.MaPhieuBaoCao.Substring(2) : phieu.MaPhieuBaoCao));
                if (maChiTietDenBu.Length > 20) maChiTietDenBu = maChiTietDenBu.Substring(0, 20);

                var denBuHienCo = await _context.CHI_TIET_DICH_VU.FirstOrDefaultAsync(c => c.MaChiTietDichVu == maChiTietDenBu);

                if (DoKhachLamHongKT && CongPhiVaoHoaDonPhongKT && !string.IsNullOrWhiteSpace(phieu.MaPhong) && (phieu.TongChiPhi ?? 0) > 0)
                {
                    var datPhongDangO = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                               join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                               where ct.PHONGMaPhong == phieu.MaPhong && dp.TrangThaiDatPhong == HotelManager.Helpers.HangSoHeThong.DatPhongDangO
                                               select dp).FirstOrDefaultAsync();

                    if (datPhongDangO != null)
                    {
                        // Đảm bảo tồn tại dịch vụ chuẩn riêng cho Phí đền bù sự cố (không lấy nhầm món ăn nhà hàng)
                        var dvDenBu = await _context.DICH_VU.FirstOrDefaultAsync(d => d.MaDichVu == "DV_DENBU_SUCO");
                        if (dvDenBu == null)
                        {
                            dvDenBu = new DICH_VU
                            {
                                MaDichVu = "DV_DENBU_SUCO",
                                TenDichVu = "Phí đền bù hư hỏng thiết bị / sự cố",
                                LoaiDichVu = "Đền bù",
                                DonGia = 0,
                                DonViTinh = "Lần",
                                ConMon = false
                            };
                            _context.DICH_VU.Add(dvDenBu);
                        }

                        if (denBuHienCo == null)
                        {
                            _context.CHI_TIET_DICH_VU.Add(new CHI_TIET_DICH_VU
                            {
                                MaChiTietDichVu = maChiTietDenBu,
                                DICH_VUMaDichVu = dvDenBu.MaDichVu,
                                DAT_PHONGMaDatPhong = datPhongDangO.MaDatPhong,
                                KHACH_HANGMaKH = datPhongDangO.KHACH_HANGMaKH,
                                PHONGMaPhong = phieu.MaPhong,
                                SoLuong = 1,
                                DonGia = phieu.TongChiPhi,
                                ThanhTien = phieu.TongChiPhi,
                                ThoiGianSuDung = now
                            });
                        }
                        else
                        {
                            // Cập nhật số tiền mới nhất thay vì chèn thêm dòng trùng lặp
                            denBuHienCo.DonGia = phieu.TongChiPhi;
                            denBuHienCo.ThanhTien = phieu.TongChiPhi;
                            denBuHienCo.DAT_PHONGMaDatPhong = datPhongDangO.MaDatPhong;
                            denBuHienCo.ThoiGianSuDung = now;
                            _context.CHI_TIET_DICH_VU.Update(denBuHienCo);
                        }
                    }
                }
                else if (denBuHienCo != null)
                {
                    // Nếu kỹ thuật bỏ tích chọn đền bù thì gỡ bỏ dòng phí cũ
                    _context.CHI_TIET_DICH_VU.Remove(denBuHienCo);
                }

                if (HanhDongKyThuat == "GuiNghiemThu")
                {
                    phieu.TrangThai = "Chờ nghiệm thu";
                    ThemThongBaoHeThong(
                        "Thông báo Nghiệm thu",
                        $"[Quản lý] Phiếu sự cố {phieu.MaPhieuBaoCao} tại {phieu.ViTriSuCo} đã được kỹ thuật sửa xong (Chi phí: {(phieu.TongChiPhi ?? 0):N0}đ). Vui lòng vào nghiệm thu!"
                    );
                }

                _context.PHIEU_BAO_CAO_SU_CO.Update(phieu);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string msgTonThap = canhBaoTonThap.Any()
                    ? $" (Cảnh báo Thủ kho tồn thấp: {string.Join(", ", canhBaoTonThap)})"
                    : "";

                TempData["SuccessMessage"] = HanhDongKyThuat == "GuiNghiemThu"
                    ? $"Đã hoàn tất sửa chữa phiếu {phieu.MaPhieuBaoCao} và gửi sang Quản lý chờ nghiệm thu!{msgTonThap}"
                    : $"Đã lưu tiến độ sửa chữa và trừ tồn kho vật tư cho phiếu {phieu.MaPhieuBaoCao}!{msgTonThap}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi cập nhật sửa chữa: " + ex.Message;
            }

            return RedirectToPage();
        }

        // 5. BƯỚC 4: Quản lý nghiệm thu (Báo xanh hoàn thành hoặc trả lại sửa lại)
        public async Task<IActionResult> OnPostNghiemThuAsync()
        {
            if (!IsQuanLy)
            {
                TempData["ErrorMessage"] = "Chỉ Quản lý mới có quyền nghiệm thu và báo xanh phiếu sự cố!";
                return RedirectToPage();
            }

            try
            {
                var phieu = await _context.PHIEU_BAO_CAO_SU_CO
                    .FirstOrDefaultAsync(p => p.MaPhieuBaoCao == MaPhieuThaoTac);

                if (phieu == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu sự cố cần nghiệm thu!";
                    return RedirectToPage();
                }

                if (phieu.TrangThai != "Chờ nghiệm thu")
                {
                    TempData["ErrorMessage"] = $"Phiếu {phieu.MaPhieuBaoCao} hiện không ở trạng thái 'Chờ nghiệm thu'!";
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;

                if (KetQuaNghiemThu == "Dat")
                {
                    phieu.TrangThai = "Hoàn thành";
                    phieu.ThoiGianHoanThanh = now;
                    _context.PHIEU_BAO_CAO_SU_CO.Update(phieu);

                    string thongBaoMoPhong = "";

                    // Mở khóa phòng Bảo trì về Trống + Vệ sinh Bẩn để Buồng phòng dọn dẹp
                    if (!string.IsNullOrWhiteSpace(phieu.MaPhong))
                    {
                        var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == phieu.MaPhong);
                        if (phong != null)
                        {
                            bool conSuCoKhacDangKhoa = await _context.PHIEU_BAO_CAO_SU_CO
                                .AnyAsync(x => x.MaPhong == phong.MaPhong
                                            && x.MaPhieuBaoCao != phieu.MaPhieuBaoCao
                                            && x.CoKhoaPhong
                                            && x.TrangThai != "Hoàn thành");

                            if (!conSuCoKhacDangKhoa)
                            {
                                var trangThaiPhong = await EnsureRoomStatusRecordAsync(phong);
                                if (string.Equals(trangThaiPhong.TrangThaiPhong?.Trim(), "Bảo trì", StringComparison.OrdinalIgnoreCase) || phieu.CoKhoaPhong)
                                {
                                    trangThaiPhong.TrangThaiPhong = "Trống";
                                    trangThaiPhong.TrangThaiVeSinh = "Bẩn";
                                    trangThaiPhong.ThoiGianDonVeSinh = now.AddDays(-1);
                                    _context.TRANG_THAI_PHONG.Update(trangThaiPhong);

                                    thongBaoMoPhong = $" Phòng {phong.SoPhong} đã tự động chuyển từ 'Bảo trì' về 'Trống' (Vệ sinh: Bẩn) và gửi thông báo cho Buồng phòng vào dọn dẹp!";

                                    ThemThongBaoHeThong(
                                        "Thông báo Buồng phòng",
                                        $"[Nhân viên Buồng phòng] Phòng {phong.SoPhong} đã sửa chữa & nghiệm thu xong (Báo xanh). Trạng thái chuyển sang Trống - Bẩn, vui lòng dọn dẹp lại phòng!"
                                    );
                                    ThemThongBaoHeThong(
                                        "Thông báo Lễ tân",
                                        $"[Lễ tân] Sự cố tại Phòng {phong.SoPhong} ({phieu.MaPhieuBaoCao}) đã nghiệm thu Hoàn thành (Báo xanh). Chờ Buồng phòng dọn sạch để mở bán."
                                    );
                                }
                            }
                        }
                    }

                    ThemThongBaoHeThong(
                        "Thông báo Kỹ thuật",
                        $"[Nhân viên Kỹ thuật] Phiếu {phieu.MaPhieuBaoCao} tại {phieu.ViTriSuCo} đã được Quản lý nghiệm thu ĐẠT (Báo xanh)."
                    );

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Nghiệm thu ĐẠT (Báo xanh) phiếu {phieu.MaPhieuBaoCao} thành công!{thongBaoMoPhong}";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(LyDoTuChoiQL))
                    {
                        TempData["ErrorMessage"] = "Vui lòng nhập rõ lý do không đạt khi trả lại phiếu cho Nhân viên Kỹ thuật sửa lại!";
                        return RedirectToPage();
                    }

                    phieu.TrangThai = "Đang sửa chữa";
                    string lichSuTuChoi = $"[{now:dd/MM HH:mm}] {LyDoTuChoiQL.Trim()}";
                    phieu.LyDoTuChoi = string.IsNullOrWhiteSpace(phieu.LyDoTuChoi)
                        ? lichSuTuChoi
                        : $"{phieu.LyDoTuChoi} | {lichSuTuChoi}";

                    if (phieu.LyDoTuChoi.Length > 500)
                    {
                        phieu.LyDoTuChoi = phieu.LyDoTuChoi.Substring(phieu.LyDoTuChoi.Length - 500);
                    }

                    _context.PHIEU_BAO_CAO_SU_CO.Update(phieu);

                    ThemThongBaoHeThong(
                        "Yêu cầu Sửa lại",
                        $"[Nhân viên Kỹ thuật] Quản lý TRẢ LẠI phiếu {phieu.MaPhieuBaoCao} ({phieu.ViTriSuCo}). Lý do: {LyDoTuChoiQL.Trim()}"
                    );

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã trả lại phiếu {phieu.MaPhieuBaoCao} về trạng thái 'Đang sửa chữa' và gửi thông báo cho Nhân viên Kỹ thuật!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi thực hiện nghiệm thu: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}