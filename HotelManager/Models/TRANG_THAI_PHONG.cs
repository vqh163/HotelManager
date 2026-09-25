using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("TRANG_THAI_PHONG")]
    public class TRANG_THAI_PHONG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaTrangThai { get; set; } = default!;

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiPhong { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiVeSinh { get; set; }
    }
}