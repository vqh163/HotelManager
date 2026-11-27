using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    // Đặt tên bảng chuẩn VIẾT_HOA_CÓ_GẠCH_DƯỚI theo quy định[cite: 17]
    [Table("LICH_LAM_VIEC")]
    public class LICH_LAM_VIEC
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaLich { get; set; } = default!;

        // Chuyển TIMESTAMP thành datetime để SQL Server hiểu đúng định dạng thời gian[cite: 17]
        [Column(TypeName = "datetime")]
        public DateTime? NgayLamViec { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public string? CaLamViec { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? GioCheckIn { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? GioCheckOut { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public string? TrangThai { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? GhiChu { get; set; }

        // Khóa ngoại liên kết tới mã nhân viên được phân công ca làm việc
        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        // Tiêu đề công việc của ca trực
        [Column(TypeName = "nvarchar(100)")]
        public string? TieuDe { get; set; }

        // Chức vụ / bộ phận tại thời điểm phân công ca
        [Column(TypeName = "nvarchar(100)")]
        public string? ChucVu { get; set; }
    }
}
