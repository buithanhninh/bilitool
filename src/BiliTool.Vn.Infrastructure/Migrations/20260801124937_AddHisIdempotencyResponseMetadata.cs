using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHisIdempotencyResponseMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "response_content_type",
                table: "his_idempotency_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "response_content_type",
                table: "his_idempotency_records");
        }
    }
}
