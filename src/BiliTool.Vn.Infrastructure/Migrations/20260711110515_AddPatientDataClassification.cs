using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDataClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_test_data",
                table: "ho_so_benh_nhan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_ho_so_benh_nhan_test_ngay_tao",
                table: "ho_so_benh_nhan",
                columns: new[] { "is_test_data", "ngay_tao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ho_so_benh_nhan_test_ngay_tao",
                table: "ho_so_benh_nhan");

            migrationBuilder.DropColumn(
                name: "is_test_data",
                table: "ho_so_benh_nhan");
        }
    }
}
