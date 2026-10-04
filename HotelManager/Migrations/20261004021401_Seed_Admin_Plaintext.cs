using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class Seed_Admin_Plaintext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TAI_KHOAN",
                keyColumn: "MaTaiKhoan",
                keyValue: "TK_ADMIN_001",
                column: "MatKhau",
                value: "123456");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TAI_KHOAN",
                keyColumn: "MaTaiKhoan",
                keyValue: "TK_ADMIN_001",
                column: "MatKhau",
                value: "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92");
        }
    }
}
