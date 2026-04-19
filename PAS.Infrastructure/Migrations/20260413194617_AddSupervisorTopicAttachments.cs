using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorTopicAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "SupervisorTopics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFilePath",
                table: "SupervisorTopics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "SupervisorTopics");

            migrationBuilder.DropColumn(
                name: "AttachmentFilePath",
                table: "SupervisorTopics");
        }
    }
}
