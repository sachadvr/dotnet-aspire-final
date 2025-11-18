using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ajouter la colonne ImageUrl seulement si elle n'existe pas déjà
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'T_Products' AND COLUMN_NAME = 'ImageUrl')
                BEGIN
                    ALTER TABLE T_Products ADD ImageUrl NVARCHAR(500) NULL;
                END
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_T_OrderItems_T_Products_ProductId",
                table: "T_OrderItems");

            migrationBuilder.AddForeignKey(
                name: "FK_T_OrderItems_T_Products_ProductId",
                table: "T_OrderItems",
                column: "ProductId",
                principalTable: "T_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_T_OrderItems_T_Products_ProductId",
                table: "T_OrderItems");

            migrationBuilder.AddForeignKey(
                name: "FK_T_OrderItems_T_Products_ProductId",
                table: "T_OrderItems",
                column: "ProductId",
                principalTable: "T_Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Supprimer la colonne ImageUrl seulement si elle existe
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'T_Products' AND COLUMN_NAME = 'ImageUrl')
                BEGIN
                    ALTER TABLE T_Products DROP COLUMN ImageUrl;
                END
            ");
        }
    }
}
