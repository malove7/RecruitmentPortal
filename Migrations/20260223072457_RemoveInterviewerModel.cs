using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInterviewerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Feedbacks; DELETE FROM Interviews;");

            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_Interviewers_InterviewerId",
                table: "Interviews");

            migrationBuilder.DropTable(
                name: "Interviewers");

            migrationBuilder.AlterColumn<string>(
                name: "InterviewerId",
                table: "Interviews",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_AspNetUsers_InterviewerId",
                table: "Interviews",
                column: "InterviewerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_AspNetUsers_InterviewerId",
                table: "Interviews");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewerId",
                table: "Interviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Interviewers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviewers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interviewers_Email",
                table: "Interviewers",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_Interviewers_InterviewerId",
                table: "Interviews",
                column: "InterviewerId",
                principalTable: "Interviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
