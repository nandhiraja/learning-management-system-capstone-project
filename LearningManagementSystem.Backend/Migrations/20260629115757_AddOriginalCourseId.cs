using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningManagementSystem.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalCourseId",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_OriginalCourseId",
                table: "Courses",
                column: "OriginalCourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Courses_OriginalCourseId",
                table: "Courses",
                column: "OriginalCourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Courses_OriginalCourseId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_OriginalCourseId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "OriginalCourseId",
                table: "Courses");
        }
    }
}
