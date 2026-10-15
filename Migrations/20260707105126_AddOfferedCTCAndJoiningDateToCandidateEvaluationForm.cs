using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferedCTCAndJoiningDateToCandidateEvaluationForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoiningDate",
                table: "CandidateEvaluationForms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferedCTC",
                table: "CandidateEvaluationForms",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoiningDate",
                table: "CandidateEvaluationForms");

            migrationBuilder.DropColumn(
                name: "OfferedCTC",
                table: "CandidateEvaluationForms");
        }
    }
}
