using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.WorkspaceDb
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "InvestigationSession",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigationSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
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
                    DetectedEntitiesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Hash);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsReasoning = table.Column<bool>(type: "boolean", nullable: false),
                    HiddenReasoning = table.Column<string>(type: "text", nullable: false),
                    InvestigationSessionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Message_InvestigationSession_InvestigationSessionId",
                        column: x => x.InvestigationSessionId,
                        principalTable: "InvestigationSession",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoredFileClassificationTag",
                columns: table => new
                {
                    ClassificationTagId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFileHash = table.Column<string>(type: "character varying(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFileClassificationTag", x => new { x.ClassificationTagId, x.StoredFileHash });
                    table.ForeignKey(
                        name: "FK_StoredFileClassificationTag_ClassificationTags_Classificati~",
                        column: x => x.ClassificationTagId,
                        principalTable: "ClassificationTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoredFileClassificationTag_StoredFiles_StoredFileHash",
                        column: x => x.StoredFileHash,
                        principalTable: "StoredFiles",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VirtualFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StoredFileHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CollectedBy = table.Column<string>(type: "text", nullable: false),
                    CollectionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CollectedLocation = table.Column<string>(type: "text", nullable: false),
                    CustomMetadataJson = table.Column<string>(type: "text", nullable: false),
                    ProposedSensitiviyLevel = table.Column<string>(type: "text", nullable: false),
                    SensitivityLevel = table.Column<string>(type: "text", nullable: false),
                    DataSensitivity = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualFiles_StoredFiles_StoredFileHash",
                        column: x => x.StoredFileHash,
                        principalTable: "StoredFiles",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AssociatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssociatedEntityType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineEvents_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceSessions",
                columns: table => new
                {
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceSessions", x => new { x.WorkspaceId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_WorkspaceSessions_InvestigationSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "InvestigationSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceSessions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceUsers",
                columns: table => new
                {
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceUsers", x => new { x.WorkspaceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_WorkspaceUsers_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: true),
                    MessageId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageAttachment_Message_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Message",
                        principalColumn: "Id");
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
                        name: "FK_ChainOfCustodyEntry_VirtualFiles_VirtualFileId",
                        column: x => x.VirtualFileId,
                        principalTable: "VirtualFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalVirtualFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessingType = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedBy = table.Column<string>(type: "text", nullable: false),
                    StoredFileHash = table.Column<string>(type: "text", nullable: false),
                    VirtualFileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedFile_VirtualFiles_VirtualFileId",
                        column: x => x.VirtualFileId,
                        principalTable: "VirtualFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceFiles",
                columns: table => new
                {
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VirtualFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceFiles", x => new { x.WorkspaceId, x.VirtualFileId });
                    table.ForeignKey(
                        name: "FK_WorkspaceFiles_VirtualFiles_VirtualFileId",
                        column: x => x.VirtualFileId,
                        principalTable: "VirtualFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceFiles_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChainOfCustodyEntry_VirtualFileId",
                table: "ChainOfCustodyEntry",
                column: "VirtualFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_InvestigationSessionId",
                table: "Message",
                column: "InvestigationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachment_MessageId",
                table: "MessageAttachment",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFile_VirtualFileId",
                table: "ProcessedFile",
                column: "VirtualFileId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFileClassificationTag_StoredFileHash",
                table: "StoredFileClassificationTag",
                column: "StoredFileHash");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_WorkspaceId",
                table: "TimelineEvents",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualFiles_StoredFileHash",
                table: "VirtualFiles",
                column: "StoredFileHash");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceFiles_VirtualFileId",
                table: "WorkspaceFiles",
                column: "VirtualFileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceSessions_SessionId",
                table: "WorkspaceSessions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChainOfCustodyEntry");

            migrationBuilder.DropTable(
                name: "MessageAttachment");

            migrationBuilder.DropTable(
                name: "ProcessedFile");

            migrationBuilder.DropTable(
                name: "StoredFileClassificationTag");

            migrationBuilder.DropTable(
                name: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "WorkspaceArtifacts");

            migrationBuilder.DropTable(
                name: "WorkspaceFiles");

            migrationBuilder.DropTable(
                name: "WorkspaceSessions");

            migrationBuilder.DropTable(
                name: "WorkspaceUsers");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "ClassificationTags");

            migrationBuilder.DropTable(
                name: "VirtualFiles");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "InvestigationSession");

            migrationBuilder.DropTable(
                name: "StoredFiles");
        }
    }
}
