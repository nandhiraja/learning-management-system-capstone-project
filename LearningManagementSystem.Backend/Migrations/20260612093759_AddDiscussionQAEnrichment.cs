using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningManagementSystem.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscussionQAEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LectureId",
                table: "Discussions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "DiscussionReplies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "DiscussionReplies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_LectureId",
                table: "Discussions",
                column: "LectureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Discussions_Lectures_LectureId",
                table: "Discussions",
                column: "LectureId",
                principalTable: "Lectures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discussions_Lectures_LectureId",
                table: "Discussions");

            migrationBuilder.DropIndex(
                name: "IX_Discussions_LectureId",
                table: "Discussions");

            migrationBuilder.DropColumn(
                name: "LectureId",
                table: "Discussions");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "DiscussionReplies");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "DiscussionReplies");
        }
    }
}
