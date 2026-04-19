using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupervisorTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResearchAreaId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedOutcomes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupervisorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupervisorTopics_AspNetUsers_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupervisorTopics_ResearchAreas_ResearchAreaId",
                        column: x => x.ResearchAreaId,
                        principalTable: "ResearchAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorTopics_ResearchAreaId",
                table: "SupervisorTopics",
                column: "ResearchAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorTopics_SupervisorId",
                table: "SupervisorTopics",
                column: "SupervisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupervisorTopics");
        }
    }
}
