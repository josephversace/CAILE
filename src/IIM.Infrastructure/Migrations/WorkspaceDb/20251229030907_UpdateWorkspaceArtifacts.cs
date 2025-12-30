using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.WorkspaceDb
{
    /// <inheritdoc />
    public partial class UpdateWorkspaceArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceArtifacts_WorkspaceId",
                table: "WorkspaceArtifacts",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceArtifacts_Workspaces_WorkspaceId",
                table: "WorkspaceArtifacts",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceArtifacts_Workspaces_WorkspaceId",
                table: "WorkspaceArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_WorkspaceArtifacts_WorkspaceId",
                table: "WorkspaceArtifacts");
        }
    }
}
