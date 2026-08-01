using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalAuditGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clinical_audit_legal_holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    placed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    placed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_audit_legal_holds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinical_audit_purge_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cutoff_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dry_run = table.Column<bool>(type: "boolean", nullable: false),
                    eligible_count = table.Column<int>(type: "integer", nullable: false),
                    protected_by_legal_hold_count = table.Column<int>(type: "integer", nullable: false),
                    deleted_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_audit_purge_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clinical_audit_legal_holds_scope",
                table: "clinical_audit_legal_holds",
                columns: new[] { "tenant_id", "result_id", "released_at" });

            migrationBuilder.CreateIndex(
                name: "ix_clinical_audit_purge_reports_executed_at",
                table: "clinical_audit_purge_reports",
                column: "executed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinical_audit_legal_holds");

            migrationBuilder.DropTable(
                name: "clinical_audit_purge_reports");
        }
    }
}
