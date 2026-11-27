using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    /// <summary>
    /// Bảng lưu trữ biên bản kiểm kê định kỳ của kho (thay thế lichsu_kiemke_meta.txt).
    /// </summary>
    [Table("BIEN_BAN_KIEM_KE")]
    public class BIEN_BAN_KIEM_KE
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaBienBan { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(150)")]
        public string KyKiemKe { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(100)")]
        public string PhamViKho { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(100)")]
        public string NguoiKiemKe { get; set; } = string.Empty;

        public int TongMatHang { get; set; }

        public int SoMatHangLech { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaTriChenhLech { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? GhiChu { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}
