using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatToanDienHeThong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LuongCoBan",
                table: "NHAN_VIEN",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChucVu",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NHAN_VIENMaNV",
                table: "LICH_LAM_VIEC",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TieuDe",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiPhucVu",
                table: "HOA_DON_NHA_HANG",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TienThueVAT",
                table: "HOA_DON_NHA_HANG",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiThanhToan",
                table: "HOA_DON_NHA_HANG",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanhBaoDiUng",
                table: "DICH_VU",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConMon",
                table: "DICH_VU",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "DICH_VU",
                type: "nvarchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DAT_PHONGMaDatPhong",
                table: "CHI_TIET_DICH_VU",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HOA_DON_NHA_HANGMaHoaDonNhaHang",
                table: "CHI_TIET_DICH_VU",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenKhachDat",
                table: "BAN_NHA_HANG",
                type: "nvarchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BIEN_BAN_KIEM_KE",
                columns: table => new
                {
                    MaBienBan = table.Column<string>(type: "varchar(20)", nullable: false),
                    KyKiemKe = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    PhamViKho = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    NguoiKiemKe = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TongMatHang = table.Column<int>(type: "int", nullable: false),
                    SoMatHangLech = table.Column<int>(type: "int", nullable: false),
                    GiaTriChenhLech = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BIEN_BAN_KIEM_KE", x => x.MaBienBan);
                });

            migrationBuilder.CreateTable(
                name: "DON_XIN_NGHI",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NHAN_VIENMaNV = table.Column<string>(type: "varchar(20)", nullable: true),
                    TenNhanVien = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ChucVu = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    SoNgay = table.Column<int>(type: "int", nullable: false),
                    TuNgay = table.Column<DateTime>(type: "date", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "date", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    IsReadByEmployee = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DON_XIN_NGHI", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PHIEU_NHAP_XUAT_KHO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    LoaiGiaoDich = table.Column<string>(type: "varchar(10)", nullable: false),
                    KHO_VAT_TUMaVatTu = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenVatTu = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    LoaiKho = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DoiTacHoacBoPhan = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    NguoiThucHien = table.Column<string>(type: "varchar(50)", nullable: true),
                    MaThamChieu = table.Column<string>(type: "varchar(20)", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHIEU_NHAP_XUAT_KHO", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "TAI_KHOAN",
                keyColumn: "MaTaiKhoan",
                keyValue: "TK_ADMIN_001",
                column: "MatKhau",
                value: "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=");

            migrationBuilder.CreateIndex(
                name: "IX_TAI_KHOAN_TenDangNhap",
                table: "TAI_KHOAN",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PHONG_SoPhong",
                table: "PHONG",
                column: "SoPhong",
                unique: true,
                filter: "[SoPhong] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PHIEU_LUU_TRU_PHONGMaPhong_TrangThaiLuuTru",
                table: "PHIEU_LUU_TRU",
                columns: new[] { "PHONGMaPhong", "TrangThaiLuuTru" });

            migrationBuilder.CreateIndex(
                name: "IX_NHAN_VIEN_SoCCCD",
                table: "NHAN_VIEN",
                column: "SoCCCD",
                unique: true,
                filter: "[SoCCCD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LICH_LAM_VIEC_NHAN_VIENMaNV",
                table: "LICH_LAM_VIEC",
                column: "NHAN_VIENMaNV");

            migrationBuilder.CreateIndex(
                name: "IX_KHACH_HANG_SoCCCD",
                table: "KHACH_HANG",
                column: "SoCCCD",
                unique: true,
                filter: "[SoCCCD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DAT_PHONG_TrangThaiDatPhong",
                table: "DAT_PHONG",
                column: "TrangThaiDatPhong");

            migrationBuilder.CreateIndex(
                name: "IX_CHI_TIET_DAT_PHONG_PHONGMaPhong",
                table: "CHI_TIET_DAT_PHONG",
                column: "PHONGMaPhong");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BIEN_BAN_KIEM_KE");

            migrationBuilder.DropTable(
                name: "DON_XIN_NGHI");

            migrationBuilder.DropTable(
                name: "PHIEU_NHAP_XUAT_KHO");

            migrationBuilder.DropIndex(
                name: "IX_TAI_KHOAN_TenDangNhap",
                table: "TAI_KHOAN");

            migrationBuilder.DropIndex(
                name: "IX_PHONG_SoPhong",
                table: "PHONG");

            migrationBuilder.DropIndex(
                name: "IX_PHIEU_LUU_TRU_PHONGMaPhong_TrangThaiLuuTru",
                table: "PHIEU_LUU_TRU");

            migrationBuilder.DropIndex(
                name: "IX_NHAN_VIEN_SoCCCD",
                table: "NHAN_VIEN");

            migrationBuilder.DropIndex(
                name: "IX_LICH_LAM_VIEC_NHAN_VIENMaNV",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropIndex(
                name: "IX_KHACH_HANG_SoCCCD",
                table: "KHACH_HANG");

            migrationBuilder.DropIndex(
                name: "IX_DAT_PHONG_TrangThaiDatPhong",
                table: "DAT_PHONG");

            migrationBuilder.DropIndex(
                name: "IX_CHI_TIET_DAT_PHONG_PHONGMaPhong",
                table: "CHI_TIET_DAT_PHONG");

            migrationBuilder.DropColumn(
                name: "LuongCoBan",
                table: "NHAN_VIEN");

            migrationBuilder.DropColumn(
                name: "ChucVu",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "NHAN_VIENMaNV",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "TieuDe",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "PhiPhucVu",
                table: "HOA_DON_NHA_HANG");

            migrationBuilder.DropColumn(
                name: "TienThueVAT",
                table: "HOA_DON_NHA_HANG");

            migrationBuilder.DropColumn(
                name: "TrangThaiThanhToan",
                table: "HOA_DON_NHA_HANG");

            migrationBuilder.DropColumn(
                name: "CanhBaoDiUng",
                table: "DICH_VU");

            migrationBuilder.DropColumn(
                name: "ConMon",
                table: "DICH_VU");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "DICH_VU");

            migrationBuilder.DropColumn(
                name: "DAT_PHONGMaDatPhong",
                table: "CHI_TIET_DICH_VU");

            migrationBuilder.DropColumn(
                name: "HOA_DON_NHA_HANGMaHoaDonNhaHang",
                table: "CHI_TIET_DICH_VU");

            migrationBuilder.AlterColumn<string>(
                name: "TenKhachDat",
                table: "BAN_NHA_HANG",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "TAI_KHOAN",
                keyColumn: "MaTaiKhoan",
                keyValue: "TK_ADMIN_001",
                column: "MatKhau",
                value: "123456");
        }
    }
}
