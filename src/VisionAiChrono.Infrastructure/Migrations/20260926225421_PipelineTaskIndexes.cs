using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisionAiChrono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PipelineTaskIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PipelineRunModel_AiModels_AiModelId",
                table: "PipelineRunModel");

            migrationBuilder.DropIndex(
                name: "IX_PipelineModels_PipelineId_AiModelId",
                table: "PipelineModels");

            migrationBuilder.AlterColumn<Guid>(
                name: "AiModelId",
                table: "PipelineRunModel",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "TaskIndex",
                table: "PipelineRunModel",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "AiModelId",
                table: "PipelineModels",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "TaskIndex",
                table: "PipelineModels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "AiModelId",
                table: "AiResults",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "TaskIndex",
                table: "AiResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PipelineModels_PipelineId_AiModelId",
                table: "PipelineModels",
                columns: new[] { "PipelineId", "AiModelId" },
                unique: true,
                filter: "[AiModelId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PipelineRunModel_AiModels_AiModelId",
                table: "PipelineRunModel",
                column: "AiModelId",
                principalTable: "AiModels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PipelineRunModel_AiModels_AiModelId",
                table: "PipelineRunModel");

            migrationBuilder.DropIndex(
                name: "IX_PipelineModels_PipelineId_AiModelId",
                table: "PipelineModels");

            migrationBuilder.DropColumn(
                name: "TaskIndex",
                table: "PipelineRunModel");

            migrationBuilder.DropColumn(
                name: "TaskIndex",
                table: "PipelineModels");

            migrationBuilder.DropColumn(
                name: "TaskIndex",
                table: "AiResults");

            migrationBuilder.AlterColumn<Guid>(
                name: "AiModelId",
                table: "PipelineRunModel",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AiModelId",
                table: "PipelineModels",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AiModelId",
                table: "AiResults",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PipelineModels_PipelineId_AiModelId",
                table: "PipelineModels",
                columns: new[] { "PipelineId", "AiModelId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PipelineRunModel_AiModels_AiModelId",
                table: "PipelineRunModel",
                column: "AiModelId",
                principalTable: "AiModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
