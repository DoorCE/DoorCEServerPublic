using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class AcquisitionAppStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Log",
                table: "AcquisitionApps",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AcquisitionApps",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Log",
                table: "AcquisitionApps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AcquisitionApps");
        }
    }
}
