using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.WorkspaceDb
{
    /// <inheritdoc />
    public partial class UpdateProcessedFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessedFiles_VirtualFiles_VirtualFileId",
                table: "ProcessedFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedFiles_StoredFileHash",
                table: "ProcessedFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedFiles_VirtualFileId",
                table: "ProcessedFiles");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "ProcessedFiles");

            migrationBuilder.DropColumn(
                name: "VirtualFileId",
                table: "ProcessedFiles");

            migrationBuilder.AddColumn<string>(
                name: "DerivedHash",
                table: "ProcessedFiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParametersHash",
                table: "ProcessedFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessorKind",
                table: "ProcessedFiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessorVersion",
                table: "ProcessedFiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_DerivedHash",
                table: "ProcessedFiles",
                column: "DerivedHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_StoredFileHash_ProcessorName_ProcessorVersio~",
                table: "ProcessedFiles",
                columns: new[] { "StoredFileHash", "ProcessorName", "ProcessorVersion", "ParametersHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedFiles_DerivedHash",
                table: "ProcessedFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedFiles_StoredFileHash_ProcessorName_ProcessorVersio~",
                table: "ProcessedFiles");

            migrationBuilder.DropColumn(
                name: "DerivedHash",
                table: "ProcessedFiles");

            migrationBuilder.DropColumn(
                name: "ParametersHash",
                table: "ProcessedFiles");

            migrationBuilder.DropColumn(
                name: "ProcessorKind",
                table: "ProcessedFiles");

            migrationBuilder.DropColumn(
                name: "ProcessorVersion",
                table: "ProcessedFiles");

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "Metadata",
                table: "ProcessedFiles",
                type: "hstore",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VirtualFileId",
                table: "ProcessedFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_StoredFileHash",
                table: "ProcessedFiles",
                column: "StoredFileHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_VirtualFileId",
                table: "ProcessedFiles",
                column: "VirtualFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessedFiles_VirtualFiles_VirtualFileId",
                table: "ProcessedFiles",
                column: "VirtualFileId",
                principalTable: "VirtualFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
