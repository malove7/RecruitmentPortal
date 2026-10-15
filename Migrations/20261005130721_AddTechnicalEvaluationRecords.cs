using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicalEvaluationRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnicalEvaluationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateEvaluationFormId = table.Column<int>(type: "int", nullable: false),
                    RecordNumber = table.Column<int>(type: "int", nullable: false),
                    TechnicalReviewerSignature = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TechnicalComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalEvaluationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalEvaluationRecords_CandidateEvaluationForms_CandidateEvaluationFormId",
                        column: x => x.CandidateEvaluationFormId,
                        principalTable: "CandidateEvaluationForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalEvaluationRecords_CandidateEvaluationFormId",
                table: "TechnicalEvaluationRecords",
                column: "CandidateEvaluationFormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicalEvaluationRecords");
        }
    }
}
