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
    }
}