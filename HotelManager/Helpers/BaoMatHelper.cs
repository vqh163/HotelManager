using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HotelManager.Helpers
{
    /// <summary>
    /// Tiện ích tĩnh phục vụ bảo mật: Băm và xác thực mật khẩu SHA-256 Base64,
    /// kiểm tra định dạng & nội dung tệp ảnh tải lên, và sinh mã khóa chính chống trùng.
    /// </summary>
    public static class BaoMatHelper
    {
        // Danh sách phần mở rộng ảnh hợp lệ được phép tải lên
        private static readonly string[] DuoiAnhHopLe = { ".jpg", ".jpeg", ".png", ".webp" };

        // Giới hạn dung lượng tối đa cho mỗi tệp ảnh tải lên (5 MB)
        private const long DungLuongToiDaBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Băm mật khẩu chuẩn SHA-256 sang chuỗi Base64 (độ dài cố định 44 ký tự).
        /// </summary>
        public static string BamMatKhauSHA256(string matKhauGoc)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(matKhauGoc));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Kiểm tra xem chuỗi lưu trong CSDL đã là chuỗi băm SHA-256 dạng Base64 hay chưa
        /// (đúng 44 ký tự và giải mã Base64 cho ra đúng 32 byte).
        /// </summary>
        public static bool LaChuoiDaBamBase64(string? chuoiLuuTru)
        {
            if (string.IsNullOrEmpty(chuoiLuuTru) || chuoiLuuTru.Length != 44)
                return false;

            Span<byte> buffer = stackalloc byte[32];
            return Convert.TryFromBase64String(chuoiLuuTru, buffer, out int bytesWritten) && bytesWritten == 32;
        }

        /// <summary>
        /// Kiểm tra xem chuỗi lưu trong CSDL có phải là chuỗi băm SHA-256 dạng Hex (64 ký tự) hay không.
        /// </summary>
        public static bool LaChuoiDaBamHex(string? chuoiLuuTru)
        {
            if (string.IsNullOrEmpty(chuoiLuuTru) || chuoiLuuTru.Length != 64)
                return false;

            return chuoiLuuTru.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        /// <summary>
        /// Xác thực mật khẩu người dùng nhập vào so với chuỗi lưu trong CSDL.
        /// Chặn hoàn toàn lỗ hổng Pass-the-Hash: nếu DB đã lưu Hash thì KHÔNG BAO GIỜ so khớp trực tiếp chuỗi gốc.
        /// </summary>
        public static bool XacThucMatKhau(string? matKhauLuuTru, string matKhauNhap, out bool canNangCapHash)
        {
            canNangCapHash = false;
            if (string.IsNullOrEmpty(matKhauLuuTru) || string.IsNullOrEmpty(matKhauNhap))
                return false;

            // Trường hợp 1: Mật khẩu trong CSDL đã ở dạng chuẩn SHA-256 Base64 (44 ký tự)
            if (LaChuoiDaBamBase64(matKhauLuuTru))
            {
                string hashNhap = BamMatKhauSHA256(matKhauNhap);
                byte[] bLuu = Encoding.UTF8.GetBytes(matKhauLuuTru);
                byte[] bNhap = Encoding.UTF8.GetBytes(hashNhap);
                return CryptographicOperations.FixedTimeEquals(bLuu, bNhap);
            }

            // Trường hợp 2: Tương thích ngược nếu có bản ghi bị băm dạng Hex (64 ký tự) từ trước
            if (LaChuoiDaBamHex(matKhauLuuTru))
            {
                string hexNhap = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(matKhauNhap)));
                bool khopHex = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(matKhauLuuTru.ToUpperInvariant()),
                    Encoding.UTF8.GetBytes(hexNhap));
                if (khopHex) canNangCapHash = true; // Nâng cấp về chuẩn Base64 thống nhất
                return khopHex;
            }

            // Trường hợp 3: Mật khẩu cũ còn lưu Plaintext -> chỉ so khớp khi DB chắc chắn chưa băm
            bool khopPlain = string.Equals(matKhauLuuTru, matKhauNhap, StringComparison.Ordinal);
            if (khopPlain) canNangCapHash = true;
            return khopPlain;
        }

        /// <summary>
        /// Sinh mã khóa chính chống trùng lặp khi có nhiều thao tác đồng thời (độ dài tối đa mặc định 20 ký tự).
        /// </summary>
        public static string SinhMaNgauNhien(string tienTo, int doDaiToiDa = 20)
        {
            string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            string randomHex = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)); // 6 ký tự ngẫu nhiên
            string ma = $"{tienTo}{timestamp}{randomHex}";
            return ma.Length > doDaiToiDa ? ma.Substring(0, doDaiToiDa) : ma;
        }

        /// <summary>
        /// Kiểm tra phần mở rộng, dung lượng và Magic Bytes của tệp ảnh, sau đó lưu an toàn vào thư mục đích.
        /// Trả về (ThanhCong, TenFileHoacLoi).
        /// </summary>
        public static async Task<(bool ThanhCong, string KetQua)> LuuAnhAnToanAsync(
            IFormFile? file,
            string thuMucVatLy,
            string tienToTenFile)
        {
            if (file == null || file.Length == 0)
                return (false, "Không có tệp ảnh được tải lên.");

            if (file.Length > DungLuongToiDaBytes)
                return (false, "Dung lượng ảnh vượt quá giới hạn cho phép (tối đa 5 MB).");

            string ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!DuoiAnhHopLe.Contains(ext))
                return (false, "Định dạng ảnh không hợp lệ! Chỉ chấp nhận các tệp .jpg, .jpeg, .png, .webp.");

            // Kiểm tra Magic Bytes đầu tệp để chống giả mạo đuôi tệp (.svg, .html, .exe đổi tên thành .jpg)
            byte[] header = new byte[12];
            using (var checkStream = file.OpenReadStream())
            {
                int bytesRead = await checkStream.ReadAsync(header, 0, header.Length);
                if (!KiemTraMagicBytesAnh(header, bytesRead, ext))
                    return (false, "Nội dung tệp không khớp với định dạng ảnh hợp lệ!");
            }

            if (!Directory.Exists(thuMucVatLy))
                Directory.CreateDirectory(thuMucVatLy);

            // Luôn tạo tên file mới bằng Guid, tuyệt đối không dùng tên gốc từ máy người dùng
            string safePrefix = string.Concat(tienToTenFile.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
            string fileName = $"{safePrefix}_{DateTime.Now:yyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, Math.Min(45, safePrefix.Length + 25)) + ext;
            string fullPath = Path.Combine(thuMucVatLy, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return (true, fileName);
        }

        /// <summary>
        /// Xác minh chữ ký byte đầu tệp cho JPEG, PNG và WEBP.
        /// </summary>
        private static bool KiemTraMagicBytesAnh(byte[] header, int length, string ext)
        {
            if (length < 4) return false;

            // JPEG: FF D8 FF
            if (ext == ".jpg" || ext == ".jpeg")
                return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (ext == ".png")
                return length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

            // WEBP: RIFF....WEBP
            if (ext == ".webp")
                return length >= 12
                    && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                    && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

            return false;
        }
    }
}
