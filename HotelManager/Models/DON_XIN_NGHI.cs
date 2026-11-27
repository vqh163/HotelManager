using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    /// <summary>
    /// Bảng lưu trữ đơn xin nghỉ phép của nhân viên trong CSDL (thay thế danh sách tĩnh trên RAM).
    /// </summary>
    [Table("DON_XIN_NGHI")]
    public class DON_XIN_NGHI
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string TenNhanVien { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(100)")]
        public string ChucVu { get; set; } = string.Empty;

        public int SoNgay { get; set; } = 1;

        [Column(TypeName = "date")]
        public DateTime TuNgay { get; set; } = DateTime.Today;

        [Column(TypeName = "date")]
        public DateTime DenNgay { get; set; } = DateTime.Today;

        [Column(TypeName = "nvarchar(500)")]
        public string LyDo { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(50)")]
        public string TrangThai { get; set; } = "Chờ duyệt";

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Column(TypeName = "nvarchar(100)")]
        public string? NguoiDuyet { get; set; }

        public bool IsReadByEmployee { get; set; } = false;
    }
}
