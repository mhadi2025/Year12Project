using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevisionPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddExamDateToSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExamDate",
                table: "Subjects",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExamDate",
                table: "Subjects");
        }
    }
}
