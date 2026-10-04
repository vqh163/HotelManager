public class LICH_LAM_VIEC
    {
        public int Id { get; set; }

        // 1. Nhóm Cơ bản
        public string MaNhanVien { get; set; } = string.Empty;
        public string TenNhanVien { get; set; } = string.Empty;
        public string BoPhan { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string LoaiCa { get; set; } = string.Empty;
        public string KhuVuc { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; } = DateTime.Today.AddHours(6);
        public DateTime ThoiGianKetThuc { get; set; } = DateTime.Today.AddHours(14);

        // 2. Nhóm Nội dung
        public string TieuDe { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty; // Mô tả
        public string CongCu { get; set; } = string.Empty; // Vật tư đi kèm
        public string Checklist { get; set; } = string.Empty; // Lưu chuỗi JSON hoặc các mục cách nhau bởi dấu |

        // 3. Nhóm Quản trị
        public string DoUuTien { get; set; } = "Bình thường"; // Bình thường, Quan trọng, Khẩn cấp
        public string TrangThai { get; set; } = "Sắp diễn ra";
        public string HinhAnh { get; set; } = string.Empty; // Đường dẫn ảnh đính kèm
    }