using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class DataItemSourceDataset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "DataItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataItems_SourceId",
                table: "DataItems",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataItems_OwnableResources_SourceId",
                table: "DataItems",
                column: "SourceId",
                principalTable: "OwnableResources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataItems_OwnableResources_SourceId",
                table: "DataItems");

            migrationBuilder.DropIndex(
                name: "IX_DataItems_SourceId",
                table: "DataItems");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "DataItems");
        }
    }
}
