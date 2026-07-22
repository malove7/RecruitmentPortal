using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddMarriedAndChildToCandidateEvaluationForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Child",
                table: "CandidateEvaluationForms",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Married",
                table: "CandidateEvaluationForms",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Child",
                table: "CandidateEvaluationForms");

            migrationBuilder.DropColumn(
                name: "Married",
                table: "CandidateEvaluationForms");
        }
    }
}
