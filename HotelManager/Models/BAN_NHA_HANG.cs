using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("BAN_NHA_HANG")]
    public class BAN_NHA_HANG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaBan { get; set; } = default!;

        [Column(TypeName = "nvarchar(50)")]
        public string? TenBan { get; set; }

        public int? SucChua { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThai { get; set; }
    }
}