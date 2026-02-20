using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentCTC",
                table: "Candidates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentLocation",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DOB",
                table: "Candidates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedCTC",
                table: "Candidates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HighestEducation",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticePeriod",
                table: "Candidates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForChange",
                table: "Candidates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCTC",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "DOB",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ExpectedCTC",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "HighestEducation",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "NoticePeriod",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ReasonForChange",
                table: "Candidates");
        }
    }
}
