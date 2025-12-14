using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevisionPlanner.Migrations
{
    /// <inheritdoc />
    public partial class TimetableUseDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeTableDay",
                table: "Timetables");

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeTableDate",
                table: "Timetables",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeTableDate",
                table: "Timetables");

            migrationBuilder.AddColumn<int>(
                name: "TimeTableDay",
                table: "Timetables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
