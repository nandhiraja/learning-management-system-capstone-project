using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningManagementSystem.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchSecondsToLectureProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "LectureProgresses",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WatchedSeconds",
                table: "LectureProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAccessedAt",
                table: "LectureProgresses");

            migrationBuilder.DropColumn(
                name: "WatchedSeconds",
                table: "LectureProgresses");
        }
    }
}
