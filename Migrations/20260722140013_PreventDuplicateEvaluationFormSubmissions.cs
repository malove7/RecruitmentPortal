using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateEvaluationFormSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionToken",
                table: "CandidateEvaluationForms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateEvaluationForms_SubmissionToken",
                table: "CandidateEvaluationForms",
                column: "SubmissionToken",
                unique: true,
                filter: "[SubmissionToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateEvaluationForms_SubmissionToken",
                table: "CandidateEvaluationForms");

            migrationBuilder.DropColumn(
                name: "SubmissionToken",
                table: "CandidateEvaluationForms");
        }
    }
}
