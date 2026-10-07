using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAIResumeScreening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIResumeScreenings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPositionId = table.Column<int>(type: "int", nullable: false),
                    ResumeFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ResumeFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OverallScore = table.Column<int>(type: "int", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecruiterSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobTitleSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobDescriptionSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobRequirementsSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CandidateExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperienceStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperienceComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AIModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawJsonResponse = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeScreenings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeScreenings_JobPositions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "JobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIResumeConcerns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AIResumeScreeningId = table.Column<int>(type: "int", nullable: false),
                    Concern = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeConcerns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeConcerns_AIResumeScreenings_AIResumeScreeningId",
                        column: x => x.AIResumeScreeningId,
                        principalTable: "AIResumeScreenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIResumeEvidences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AIResumeScreeningId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeEvidences_AIResumeScreenings_AIResumeScreeningId",
                        column: x => x.AIResumeScreeningId,
                        principalTable: "AIResumeScreenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIResumeFollowUpQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AIResumeScreeningId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeFollowUpQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeFollowUpQuestions_AIResumeScreenings_AIResumeScreeningId",
                        column: x => x.AIResumeScreeningId,
                        principalTable: "AIResumeScreenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIResumeGaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AIResumeScreeningId = table.Column<int>(type: "int", nullable: false),
                    Gap = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeGaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeGaps_AIResumeScreenings_AIResumeScreeningId",
                        column: x => x.AIResumeScreeningId,
                        principalTable: "AIResumeScreenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIResumeSkillEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AIResumeScreeningId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GapsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeSkillEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeSkillEvaluations_AIResumeScreenings_AIResumeScreeningId",
                        column: x => x.AIResumeScreeningId,
                        principalTable: "AIResumeScreenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIResumeStrengths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AIResumeScreeningId = table.Column<int>(type: "int", nullable: false),
                    Strength = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResumeStrengths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIResumeStrengths_AIResumeScreenings_AIResumeScreeningId",
                        column: x => x.AIResumeScreeningId,
                        principalTable: "AIResumeScreenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeConcerns_AIResumeScreeningId",
                table: "AIResumeConcerns",
                column: "AIResumeScreeningId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeEvidences_AIResumeScreeningId",
                table: "AIResumeEvidences",
                column: "AIResumeScreeningId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeFollowUpQuestions_AIResumeScreeningId",
                table: "AIResumeFollowUpQuestions",
                column: "AIResumeScreeningId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeGaps_AIResumeScreeningId",
                table: "AIResumeGaps",
                column: "AIResumeScreeningId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeScreenings_JobPositionId",
                table: "AIResumeScreenings",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeSkillEvaluations_AIResumeScreeningId",
                table: "AIResumeSkillEvaluations",
                column: "AIResumeScreeningId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResumeStrengths_AIResumeScreeningId",
                table: "AIResumeStrengths",
                column: "AIResumeScreeningId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIResumeConcerns");

            migrationBuilder.DropTable(
                name: "AIResumeEvidences");

            migrationBuilder.DropTable(
                name: "AIResumeFollowUpQuestions");

            migrationBuilder.DropTable(
                name: "AIResumeGaps");

            migrationBuilder.DropTable(
                name: "AIResumeSkillEvaluations");

            migrationBuilder.DropTable(
                name: "AIResumeStrengths");

            migrationBuilder.DropTable(
                name: "AIResumeScreenings");
        }
    }
}
