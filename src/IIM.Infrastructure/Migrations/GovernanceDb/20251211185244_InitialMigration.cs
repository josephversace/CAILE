using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.GovernanceDb
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "AccessRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<int>(type: "integer", nullable: false),
                    EncryptionRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SeaweedFSCollection = table.Column<string>(type: "text", nullable: false),
                    RetentionPeriodDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredFile",
                columns: table => new
                {
                    Blake3Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Md5Hash = table.Column<string>(type: "text", nullable: true),
                    Sha256Hash = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    QuarantineReason = table.Column<string>(type: "text", nullable: false),
                    QuarantinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Bucket = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstSeenBy = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    FirstWorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerceptualHash = table.Column<string>(type: "text", nullable: true),
                    PerceptualQuality = table.Column<double>(type: "double precision", nullable: true),
                    ContentSummary = table.Column<string>(type: "text", nullable: true),
                    DetectedEntitiesJson = table.Column<string>(type: "text", nullable: true),
                    IsIndexed = table.Column<bool>(type: "boolean", nullable: false),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false),
                    EntityCount = table.Column<int>(type: "integer", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GraphRagMetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFile", x => x.Blake3Hash);
                });

            migrationBuilder.CreateTable(
                name: "AccessControlRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassificationTagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessControlRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessControlRules_AccessRoles_AccessRoleId",
                        column: x => x.AccessRoleId,
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessControlRules_ClassificationTags_ClassificationTagId",
                        column: x => x.ClassificationTagId,
                        principalTable: "ClassificationTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoredFileClassificationTags",
                columns: table => new
                {
                    ClassificationTagsId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFilesBlake3Hash = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFileClassificationTags", x => new { x.ClassificationTagsId, x.StoredFilesBlake3Hash });
                    table.ForeignKey(
                        name: "FK_StoredFileClassificationTags_ClassificationTags_Classificat~",
                        column: x => x.ClassificationTagsId,
                        principalTable: "ClassificationTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoredFileClassificationTags_StoredFile_StoredFilesBlake3Ha~",
                        column: x => x.StoredFilesBlake3Hash,
                        principalTable: "StoredFile",
                        principalColumn: "Blake3Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VirtualFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StoredFileHash = table.Column<string>(type: "text", nullable: true),
                    StoredFileBlake3Hash = table.Column<string>(type: "character varying(64)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProposedLabel = table.Column<string>(type: "text", nullable: true),
                    EnrichmentMetadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CustomMetadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CustomMetadataJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualFile_StoredFile_StoredFileBlake3Hash",
                        column: x => x.StoredFileBlake3Hash,
                        principalTable: "StoredFile",
                        principalColumn: "Blake3Hash");
                });

            migrationBuilder.CreateTable(
                name: "ChainOfCustodyEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VirtualFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainOfCustodyEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChainOfCustodyEntry_VirtualFile_VirtualFileId",
                        column: x => x.VirtualFileId,
                        principalTable: "VirtualFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VirtualFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFileHash = table.Column<string>(type: "text", nullable: false),
                    StoredFileBlake3Hash = table.Column<string>(type: "character varying(64)", nullable: true),
                    ProcessorName = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedFile_StoredFile_StoredFileBlake3Hash",
                        column: x => x.StoredFileBlake3Hash,
                        principalTable: "StoredFile",
                        principalColumn: "Blake3Hash");
                    table.ForeignKey(
                        name: "FK_ProcessedFile_VirtualFile_VirtualFileId",
                        column: x => x.VirtualFileId,
                        principalTable: "VirtualFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlRules_AccessRoleId",
                table: "AccessControlRules",
                column: "AccessRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlRules_ClassificationTagId",
                table: "AccessControlRules",
                column: "ClassificationTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ChainOfCustodyEntry_VirtualFileId",
                table: "ChainOfCustodyEntry",
                column: "VirtualFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFile_StoredFileBlake3Hash",
                table: "ProcessedFile",
                column: "StoredFileBlake3Hash");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFile_VirtualFileId",
                table: "ProcessedFile",
                column: "VirtualFileId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFileClassificationTags_StoredFilesBlake3Hash",
                table: "StoredFileClassificationTags",
                column: "StoredFilesBlake3Hash");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualFile_StoredFileBlake3Hash",
                table: "VirtualFile",
                column: "StoredFileBlake3Hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessControlRules");

            migrationBuilder.DropTable(
                name: "ChainOfCustodyEntry");

            migrationBuilder.DropTable(
                name: "ProcessedFile");

            migrationBuilder.DropTable(
                name: "StorageTiers");

            migrationBuilder.DropTable(
                name: "StoredFileClassificationTags");

            migrationBuilder.DropTable(
                name: "AccessRoles");

            migrationBuilder.DropTable(
                name: "VirtualFile");

            migrationBuilder.DropTable(
                name: "ClassificationTags");

            migrationBuilder.DropTable(
                name: "StoredFile");
        }
    }
}
