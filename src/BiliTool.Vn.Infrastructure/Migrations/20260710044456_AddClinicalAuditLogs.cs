using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clinical_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guideline_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    engine_mode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    api_client_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    request_json = table.Column<string>(type: "jsonb", nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: false),
                    trace_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clinical_audit_logs_calculated_at",
                table: "clinical_audit_logs",
                column: "calculated_at");

            migrationBuilder.CreateIndex(
                name: "ix_clinical_audit_logs_guideline_code",
                table: "clinical_audit_logs",
                column: "guideline_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinical_audit_logs");
        }
    }
}
