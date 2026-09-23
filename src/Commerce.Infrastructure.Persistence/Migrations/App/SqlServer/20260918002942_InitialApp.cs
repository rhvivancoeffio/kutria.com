using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commerce.Infrastructure.Persistence.Migrations.App.SqlServer
{
    /// <inheritdoc />
    public partial class InitialApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StripeInvoiceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrainIngestJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BrainKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    TableCount = table.Column<int>(type: "int", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EvaluationFailedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrainIngestJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "McpOAuthAuthorizationCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Resource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpOAuthAuthorizationCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "McpOAuthClients",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RedirectUrisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrantTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpOAuthClients", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "McpOAuthRefreshTokens",
                columns: table => new
                {
                    Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Resource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpOAuthRefreshTokens", x => x.Token);
                });

            migrationBuilder.CreateTable(
                name: "PolicyEvalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BaseItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyEvalItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyEvalRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyEvalRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Impact = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAgentDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Queue = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Tools = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Publishes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Yaml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasedOnHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAgentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantBillings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TrialEndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBillings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantOwners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantOwners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    EnvironmentKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyEvalAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ToolResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Invented = table.Column<bool>(type: "bit", nullable: false),
                    JudgeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyEvalAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyEvalAnswers_PolicyEvalRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "PolicyEvalRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_TenantId",
                table: "BillingInvoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_TenantId_InvoiceNumber",
                table: "BillingInvoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrainIngestJobs_TenantId_BrainKey_Status",
                table: "BrainIngestJobs",
                columns: new[] { "TenantId", "BrainKey", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_TenantId",
                table: "Integrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_TenantId_WorkspaceId_Provider_Name",
                table: "Integrations",
                columns: new[] { "TenantId", "WorkspaceId", "Provider", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_McpOAuthAuthorizationCodes_Code_ClientId",
                table: "McpOAuthAuthorizationCodes",
                columns: new[] { "Code", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_McpOAuthRefreshTokens_Token_ClientId",
                table: "McpOAuthRefreshTokens",
                columns: new[] { "Token", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalAnswers_RunId",
                table: "PolicyEvalAnswers",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalItems_TenantId_BaseItemId",
                table: "PolicyEvalItems",
                columns: new[] { "TenantId", "BaseItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvalRuns_TenantId_JobId_CreatedAt",
                table: "PolicyEvalRuns",
                columns: new[] { "TenantId", "JobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_TenantId_Status",
                table: "Proposals",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAgentDefinitions_TenantId_AgentKey",
                table: "TenantAgentDefinitions",
                columns: new[] { "TenantId", "AgentKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantApiKeys_KeyHash",
                table: "TenantApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantApiKeys_TenantId",
                table: "TenantApiKeys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantBillings_TenantId",
                table: "TenantBillings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantOwners_Email",
                table: "TenantOwners",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantOwners_TenantId",
                table: "TenantOwners",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_TenantId",
                table: "Workspaces",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_TenantId_Name",
                table: "Workspaces",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingInvoices");

            migrationBuilder.DropTable(
                name: "BrainIngestJobs");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "McpOAuthAuthorizationCodes");

            migrationBuilder.DropTable(
                name: "McpOAuthClients");

            migrationBuilder.DropTable(
                name: "McpOAuthRefreshTokens");

            migrationBuilder.DropTable(
                name: "PolicyEvalAnswers");

            migrationBuilder.DropTable(
                name: "PolicyEvalItems");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "TenantAgentDefinitions");

            migrationBuilder.DropTable(
                name: "TenantApiKeys");

            migrationBuilder.DropTable(
                name: "TenantBillings");

            migrationBuilder.DropTable(
                name: "TenantOwners");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "PolicyEvalRuns");
        }
    }
}
