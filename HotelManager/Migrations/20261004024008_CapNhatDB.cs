using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CHUC_VU",
                columns: new[] { "MaChucVu", "PHONG_BANMaPB", "TenChucVu" },
                values: new object[] { "CV13", null, "Khách hàng" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV13");
        }
    }
}
