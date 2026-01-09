using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.WorkspaceDb
{
    /// <inheritdoc />
    public partial class IngestionStepState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngestionStepStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    VirtualFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StepVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OutputHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ParametersHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    IsFatal = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeferred = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionStepStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionStepStates_StoredFileHash_Status",
                table: "IngestionStepStates",
                columns: new[] { "StoredFileHash", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionStepStates_StoredFileHash_StepId_StepVersion_Input~",
                table: "IngestionStepStates",
                columns: new[] { "StoredFileHash", "StepId", "StepVersion", "InputHash", "ParametersHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestionStepStates_UpdatedAt",
                table: "IngestionStepStates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionStepStates_WorkspaceId_VirtualFileId",
                table: "IngestionStepStates",
                columns: new[] { "WorkspaceId", "VirtualFileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngestionStepStates");
        }
    }
}
