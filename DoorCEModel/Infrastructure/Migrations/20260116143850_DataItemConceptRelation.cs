using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class DataItemConceptRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConceptUri",
                table: "DataItems");

            migrationBuilder.AddColumn<int>(
                name: "ConceptId",
                table: "DataItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DataItems_ConceptId",
                table: "DataItems",
                column: "ConceptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataItems_NamespaceElements_ConceptId",
                table: "DataItems",
                column: "ConceptId",
                principalTable: "NamespaceElements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataItems_NamespaceElements_ConceptId",
                table: "DataItems");

            migrationBuilder.DropIndex(
                name: "IX_DataItems_ConceptId",
                table: "DataItems");

            migrationBuilder.DropColumn(
                name: "ConceptId",
                table: "DataItems");

            migrationBuilder.AddColumn<string>(
                name: "ConceptUri",
                table: "DataItems",
                type: "text",
                nullable: true);
        }
    }
}
