using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "T_Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "T_Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_Products_CategoryId",
                table: "T_Products",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_T_Products_T_Categories_CategoryId",
                table: "T_Products",
                column: "CategoryId",
                principalTable: "T_Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_T_Products_T_Categories_CategoryId",
                table: "T_Products");

            migrationBuilder.DropTable(
                name: "T_Categories");

            migrationBuilder.DropIndex(
                name: "IX_T_Products_CategoryId",
                table: "T_Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "T_Products");
        }
    }
}
