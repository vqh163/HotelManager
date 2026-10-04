using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatDB2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TRANG_THAI_PHONGMaTrangThai",
                table: "PHONG",
                newName: "MaTrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaTrangThai",
                table: "PHONG",
                newName: "TRANG_THAI_PHONGMaTrangThai");
        }
    }
}
