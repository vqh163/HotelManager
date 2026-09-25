using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class ThemKhoaNgoaiTaiKhoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KHACH_HANGMaKH",
                table: "TAI_KHOAN",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NHAN_VIENMaNV",
                table: "TAI_KHOAN",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KHACH_HANGMaKH",
                table: "TAI_KHOAN");

            migrationBuilder.DropColumn(
                name: "NHAN_VIENMaNV",
                table: "TAI_KHOAN");
        }
    }
}
