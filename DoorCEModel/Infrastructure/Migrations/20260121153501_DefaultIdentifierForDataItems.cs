using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class DefaultIdentifierForDataItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultIdentifier",
                table: "NamespaceElements",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultIdentifier",
                table: "NamespaceElements");
        }
    }
}
