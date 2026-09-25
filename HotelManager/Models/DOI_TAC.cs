using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("DOI_TAC")]
    public class DOI_TAC
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaDoiTac { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? TenDoiTac { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? LoaiDoiTac { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string? NguoiLienHe { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? SDT { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? Email { get; set; }

        public double? ChietKhau { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? HanMucCongNo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SoDuCongNo { get; set; }
    }
}