using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("CHI_TIET_DICH_VU")]
    public class CHI_TIET_DICH_VU
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaChiTietDichVu { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? DICH_VUMaDichVu { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? KHACH_HANGMaKH { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? PHONGMaPhong { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? KHO_VAT_TUMaVatTu { get; set; }

        public int? SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DonGia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ThanhTien { get; set; }
        public DateTime? ThoiGianSuDung { get; set; }
    }
}