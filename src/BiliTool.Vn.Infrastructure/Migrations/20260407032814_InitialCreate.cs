using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phien_lam_viec",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ngay_tao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dia_chi_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    thiet_bi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    nguoi_dung_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phien_lam_viec", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lich_su_tinh_toan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tuoi_gio = table.Column<int>(type: "integer", nullable: false),
                    tuoi_thai_tuan = table.Column<int>(type: "integer", nullable: false),
                    bilirubin_mgdl = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    co_nguyen_co_than_kinh = table.Column<bool>(type: "boolean", nullable: false),
                    nguong_chieu_den = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    nguong_chieu_den_tich_cuc = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    nguong_thay_cuu_mau = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    muc_do_nguy_hiem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    khuyen_nghi_chinh = table.Column<string>(type: "text", nullable: false),
                    ngay_tinh_toan = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_su_tinh_toan", x => x.id);
                    table.ForeignKey(
                        name: "FK_lich_su_tinh_toan_phien_lam_viec_phien_id",
                        column: x => x.phien_id,
                        principalTable: "phien_lam_viec",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mau_bilirubin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    thoi_gian_lay_mau = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    bilirubin_mgdl = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tuoi_gio_khi_lay_mau = table.Column<int>(type: "integer", nullable: false),
                    toc_do_thay_doi = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mau_bilirubin", x => x.id);
                    table.ForeignKey(
                        name: "FK_mau_bilirubin_phien_lam_viec_phien_id",
                        column: x => x.phien_id,
                        principalTable: "phien_lam_viec",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_tinh_toan_phien_id",
                table: "lich_su_tinh_toan",
                column: "phien_id");

            migrationBuilder.CreateIndex(
                name: "IX_mau_bilirubin_phien_id",
                table: "mau_bilirubin",
                column: "phien_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lich_su_tinh_toan");

            migrationBuilder.DropTable(
                name: "mau_bilirubin");

            migrationBuilder.DropTable(
                name: "phien_lam_viec");
        }
    }
}
