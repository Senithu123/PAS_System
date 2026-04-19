using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPreferencesOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectProposalId = table.Column<int>(type: "int", nullable: false),
                    PreferenceRank = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPreferences_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentPreferences_ProjectProposals_ProjectProposalId",
                        column: x => x.ProjectProposalId,
                        principalTable: "ProjectProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentPreferences_ProjectProposalId",
                table: "StudentPreferences",
                column: "ProjectProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPreferences_StudentId",
                table: "StudentPreferences",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentPreferences");
        }
    }
}
