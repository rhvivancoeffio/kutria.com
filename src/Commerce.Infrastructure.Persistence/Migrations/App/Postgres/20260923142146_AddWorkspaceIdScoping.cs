using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commerce.Infrastructure.Persistence.Migrations.App.Postgres
{
    /// <inheritdoc />
    public partial class AddWorkspaceIdScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantApiKeys_TenantId",
                table: "TenantApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_PolicyEvalRuns_TenantId_JobId_CreatedAt",
                table: "PolicyEvalRuns");

            migrationBuilder.DropIndex(
                name: "IX_PolicyEvalItems_TenantId_BaseItemId",
                table: "PolicyEvalItems");

            migrationBuilder.DropIndex(
                name: "IX_BrainIngestJobs_TenantId_BrainKey_Status",
                table: "BrainIngestJobs");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "TenantApiKeys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "PolicyEvalRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "PolicyEvalItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "BrainIngestJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantApiKeys_TenantId_WorkspaceId",
                table: "TenantApiKeys",
                columns: new[] { "TenantId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalRuns_TenantId_WorkspaceId_JobId_CreatedAt",
                table: "PolicyEvalRuns",
                columns: new[] { "TenantId", "WorkspaceId", "JobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalItems_TenantId_WorkspaceId_BaseItemId",
                table: "PolicyEvalItems",
                columns: new[] { "TenantId", "WorkspaceId", "BaseItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BrainIngestJobs_TenantId_WorkspaceId_BrainKey_Status",
                table: "BrainIngestJobs",
                columns: new[] { "TenantId", "WorkspaceId", "BrainKey", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantApiKeys_TenantId_WorkspaceId",
                table: "TenantApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_PolicyEvalRuns_TenantId_WorkspaceId_JobId_CreatedAt",
                table: "PolicyEvalRuns");

            migrationBuilder.DropIndex(
                name: "IX_PolicyEvalItems_TenantId_WorkspaceId_BaseItemId",
                table: "PolicyEvalItems");

            migrationBuilder.DropIndex(
                name: "IX_BrainIngestJobs_TenantId_WorkspaceId_BrainKey_Status",
                table: "BrainIngestJobs");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "TenantApiKeys");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "PolicyEvalRuns");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "PolicyEvalItems");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "BrainIngestJobs");

            migrationBuilder.CreateIndex(
                name: "IX_TenantApiKeys_TenantId",
                table: "TenantApiKeys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalRuns_TenantId_JobId_CreatedAt",
                table: "PolicyEvalRuns",
                columns: new[] { "TenantId", "JobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalItems_TenantId_BaseItemId",
                table: "PolicyEvalItems",
                columns: new[] { "TenantId", "BaseItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BrainIngestJobs_TenantId_BrainKey_Status",
                table: "BrainIngestJobs",
                columns: new[] { "TenantId", "BrainKey", "Status" });
        }
    }
}
