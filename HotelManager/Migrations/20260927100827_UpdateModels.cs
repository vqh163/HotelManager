using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LICH_LAM_VIEC",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "MaLich",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "CaLamViec",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "GioCheckIn",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "GioCheckOut",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "NHAN_VIENMaNV",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "NgayLamViec",
                table: "LICH_LAM_VIEC");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GhiChu",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "LICH_LAM_VIEC",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "BoPhan",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChucVu",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KhuVuc",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoaiCa",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaNhanVien",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenNhanVien",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianBatDau",
                table: "LICH_LAM_VIEC",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianKetThuc",
                table: "LICH_LAM_VIEC",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TieuDe",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LICH_LAM_VIEC",
                table: "LICH_LAM_VIEC",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LICH_LAM_VIEC",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "BoPhan",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "ChucVu",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "KhuVuc",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "LoaiCa",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "MaNhanVien",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "TenNhanVien",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "ThoiGianBatDau",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "ThoiGianKetThuc",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "TieuDe",
                table: "LICH_LAM_VIEC");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "GhiChu",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "MaLich",
                table: "LICH_LAM_VIEC",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CaLamViec",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GioCheckIn",
                table: "LICH_LAM_VIEC",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GioCheckOut",
                table: "LICH_LAM_VIEC",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NHAN_VIENMaNV",
                table: "LICH_LAM_VIEC",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayLamViec",
                table: "LICH_LAM_VIEC",
                type: "date",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LICH_LAM_VIEC",
                table: "LICH_LAM_VIEC",
                column: "MaLich");
        }
    }
}
