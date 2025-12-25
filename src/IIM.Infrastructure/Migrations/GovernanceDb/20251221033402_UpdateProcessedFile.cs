using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIM.Infrastructure.Migrations.GovernanceDb
{
    /// <inheritdoc />
    public partial class UpdateProcessedFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessedFile_VirtualFile_VirtualFileId",
                table: "ProcessedFile");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedFile_VirtualFileId",
                table: "ProcessedFile");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "ProcessedFile");

            migrationBuilder.DropColumn(
                name: "VirtualFileId",
                table: "ProcessedFile");

            migrationBuilder.AddColumn<string>(
                name: "DerivedHash",
                table: "ProcessedFile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParametersHash",
                table: "ProcessedFile",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessorKind",
                table: "ProcessedFile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessorVersion",
                table: "ProcessedFile",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DerivedHash",
                table: "ProcessedFile");

            migrationBuilder.DropColumn(
                name: "ParametersHash",
                table: "ProcessedFile");

            migrationBuilder.DropColumn(
                name: "ProcessorKind",
                table: "ProcessedFile");

            migrationBuilder.DropColumn(
                name: "ProcessorVersion",
                table: "ProcessedFile");

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "Metadata",
                table: "ProcessedFile",
                type: "hstore",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VirtualFileId",
                table: "ProcessedFile",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFile_VirtualFileId",
                table: "ProcessedFile",
                column: "VirtualFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessedFile_VirtualFile_VirtualFileId",
                table: "ProcessedFile",
                column: "VirtualFileId",
                principalTable: "VirtualFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
