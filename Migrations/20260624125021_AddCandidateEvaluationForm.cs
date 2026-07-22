using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateEvaluationForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateEvaluationForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InterviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InterviewTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PositionAppliedFor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TotalExperience = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelevantIndustryExperience = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ResidenceAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentOrganization = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NoticePeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreviouslyInterviewed = table.Column<bool>(type: "bit", nullable: true),
                    Declaration = table.Column<bool>(type: "bit", nullable: false),
                    DigitalSignature = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CommunicationSkills = table.Column<int>(type: "int", nullable: true),
                    Confidence = table.Column<int>(type: "int", nullable: true),
                    HRComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HRSignature = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TechnicalComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TechnicalReviewerSignature = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OtherComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateEvaluationForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateEvaluationFormId = table.Column<int>(type: "int", nullable: false),
                    RecordNumber = table.Column<int>(type: "int", nullable: false),
                    DegreeCourse = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    YearOfPassing = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DivisionPercentage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationRecords_CandidateEvaluationForms_CandidateEvaluationFormId",
                        column: x => x.CandidateEvaluationFormId,
                        principalTable: "CandidateEvaluationForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkExperienceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateEvaluationFormId = table.Column<int>(type: "int", nullable: false),
                    RecordNumber = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationOfWork = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastCTCPerAnnum = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkExperienceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkExperienceRecords_CandidateEvaluationForms_CandidateEvaluationFormId",
                        column: x => x.CandidateEvaluationFormId,
                        principalTable: "CandidateEvaluationForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationRecords_CandidateEvaluationFormId",
                table: "EducationRecords",
                column: "CandidateEvaluationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperienceRecords_CandidateEvaluationFormId",
                table: "WorkExperienceRecords",
                column: "CandidateEvaluationFormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationRecords");

            migrationBuilder.DropTable(
                name: "WorkExperienceRecords");

            migrationBuilder.DropTable(
                name: "CandidateEvaluationForms");
        }
    }
}
