using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    // Đảm bảo namespace trùng khớp với cấu trúc thư mục
    [Table("NHAT_KY_HE_THONG")]
    public class NHAT_KY_HE_THONG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaNhatKy { get; set; } = default!;

        [Column(TypeName = "varchar(50)")]
        public string? TenDangNhap { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string HanhDong { get; set; } = default!;

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [Column(TypeName = "nvarchar(MAX)")]
        public string? ChiTiet { get; set; }
    }
}