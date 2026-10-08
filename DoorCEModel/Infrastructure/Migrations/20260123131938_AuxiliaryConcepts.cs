using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class AuxiliaryConcepts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppTemplateId",
                table: "NamespaceElements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceElements_AppTemplateId",
                table: "NamespaceElements",
                column: "AppTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_NamespaceElements_AppTemplates_AppTemplateId",
                table: "NamespaceElements",
                column: "AppTemplateId",
                principalTable: "AppTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NamespaceElements_AppTemplates_AppTemplateId",
                table: "NamespaceElements");

            migrationBuilder.DropIndex(
                name: "IX_NamespaceElements_AppTemplateId",
                table: "NamespaceElements");

            migrationBuilder.DropColumn(
                name: "AppTemplateId",
                table: "NamespaceElements");
        }
    }
}
