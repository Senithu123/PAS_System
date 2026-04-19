using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsProposalSubmissionOpen = table.Column<bool>(type: "bit", nullable: false),
                    IsTopicPublishingOpen = table.Column<bool>(type: "bit", nullable: false),
                    IsMatchingOpen = table.Column<bool>(type: "bit", nullable: false),
                    AllowFileUploads = table.Column<bool>(type: "bit", nullable: false),
                    MaxPreferencesPerStudent = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
