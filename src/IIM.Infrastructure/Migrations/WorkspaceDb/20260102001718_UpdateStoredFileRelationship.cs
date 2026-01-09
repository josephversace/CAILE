using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.WorkspaceDb
{
    /// <inheritdoc />
    public partial class UpdateStoredFileRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StoredFileHash",
                table: "VirtualFiles",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StoredFileHash",
                table: "VirtualFiles",
                type: "character varying(64)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)");
        }
    }
}
