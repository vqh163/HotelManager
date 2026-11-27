using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    /// <summary>
    /// Bảng lưu trữ lịch sử các phiếu Nhập kho / Xuất kho / Cân bằng kiểm kê (thay thế lichsu_nhapxuat_meta.txt).
    /// </summary>
    [Table("PHIEU_NHAP_XUAT_KHO")]
    public class PHIEU_NHAP_XUAT_KHO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string MaPhieu { get; set; } = string.Empty;

        // "NHAP" hoặc "XUAT"
        [Required]
        [Column(TypeName = "varchar(10)")]
        public string LoaiGiaoDich { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string KHO_VAT_TUMaVatTu { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(100)")]
        public string TenVatTu { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(50)")]
        public string LoaiKho { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(20)")]
        public string DonViTinh { get; set; } = string.Empty;

        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [Column(TypeName = "nvarchar(150)")]
        public string DoiTacHoacBoPhan { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(500)")]
        public string? GhiChu { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? NguoiThucHien { get; set; }

        // Mã tham chiếu (VD: Mã phiếu báo cáo sự cố hoặc Mã biên bản kiểm kê)
        [Column(TypeName = "varchar(20)")]
        public string? MaThamChieu { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}
