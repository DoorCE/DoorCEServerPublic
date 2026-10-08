using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class CodeContents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodePackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    CodeFramework = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodePackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodePackages_AppTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "AppTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "text", nullable: false),
                    CodeContents = table.Column<string>(type: "text", nullable: false),
                    PackageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeFiles_CodePackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "CodePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeFiles_PackageId",
                table: "CodeFiles",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CodePackages_TemplateId",
                table: "CodePackages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CodePackages_Uri",
                table: "CodePackages",
                column: "Uri",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeFiles");

            migrationBuilder.DropTable(
                name: "CodePackages");
        }
    }
}
