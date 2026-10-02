using System;

namespace HotelManager.Models
{
    public class LICH_LAM_VIEC
    {
        public int Id { get; set; }

        // Thông tin nhân viên
        public string MaNhanVien { get; set; } = string.Empty;
        public string TenNhanVien { get; set; } = string.Empty;
        public string BoPhan { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;

        // Thông tin ca làm việc
        public string TieuDe { get; set; } = string.Empty;
        public string LoaiCa { get; set; } = string.Empty;
        public string KhuVuc { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string TrangThai { get; set; } = "Sắp diễn ra";

        // Chi tiết công việc
        public string GhiChu { get; set; } = string.Empty;
    }
}