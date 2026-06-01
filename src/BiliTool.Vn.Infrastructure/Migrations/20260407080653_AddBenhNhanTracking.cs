using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBenhNhanTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ho_so_benh_nhan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguoi_dung_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ho_ten_benh_nhan = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ngay_gio_sinh = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tuoi_thai_tuan = table.Column<int>(type: "integer", nullable: false),
                    co_nguon_co_than_kinh = table.Column<bool>(type: "boolean", nullable: false),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    ngay_tao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ho_so_benh_nhan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "xet_nghiem_bilirubin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    benh_nhan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thoi_gian_lay_mau = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    bilirubin_mgdl = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tuoi_gio_tu_dong = table.Column<double>(type: "double precision", nullable: false),
                    muc_do_nguy_hiem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    nguong_chieu_den = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    nguong_thay_cuu_mau = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ngay_tao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xet_nghiem_bilirubin", x => x.id);
                    table.ForeignKey(
                        name: "FK_xet_nghiem_bilirubin_ho_so_benh_nhan_benh_nhan_id",
                        column: x => x.benh_nhan_id,
                        principalTable: "ho_so_benh_nhan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ho_so_benh_nhan_nguoi_dung_id",
                table: "ho_so_benh_nhan",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "IX_xet_nghiem_bilirubin_benh_nhan_id",
                table: "xet_nghiem_bilirubin",
                column: "benh_nhan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "xet_nghiem_bilirubin");

            migrationBuilder.DropTable(
                name: "ho_so_benh_nhan");
        }
    }
}
