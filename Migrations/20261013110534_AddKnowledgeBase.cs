using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeBases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBases_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnowledgeBaseId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseChatSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseChatSessions_KnowledgeBases_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "KnowledgeBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnowledgeBaseId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedCharCount = table.Column<int>(type: "int", nullable: true),
                    ChunkCount = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseDocuments_KnowledgeBases_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "KnowledgeBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CitationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseChatMessages_KnowledgeBaseChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "KnowledgeBaseChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnowledgeBaseDocumentId = table.Column<int>(type: "int", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    VectorId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseChunks_KnowledgeBaseDocuments_KnowledgeBaseDocumentId",
                        column: x => x.KnowledgeBaseDocumentId,
                        principalTable: "KnowledgeBaseDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseChatMessages_ChatSessionId",
                table: "KnowledgeBaseChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseChatSessions_KnowledgeBaseId",
                table: "KnowledgeBaseChatSessions",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseChatSessions_UserId",
                table: "KnowledgeBaseChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseChunks_KnowledgeBaseDocumentId",
                table: "KnowledgeBaseChunks",
                column: "KnowledgeBaseDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseChunks_VectorId",
                table: "KnowledgeBaseChunks",
                column: "VectorId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseDocuments_KnowledgeBaseId",
                table: "KnowledgeBaseDocuments",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseDocuments_UploadedByUserId",
                table: "KnowledgeBaseDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBases_CreatedByUserId",
                table: "KnowledgeBases",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeBaseChatMessages");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseChunks");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseChatSessions");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseDocuments");

            migrationBuilder.DropTable(
                name: "KnowledgeBases");
        }
    }
}
