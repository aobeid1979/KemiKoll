using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemiKoll.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    hazard_statements = table.Column<string[]>(type: "text[]", nullable: false),
                    pictograms = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "safety_data_sheets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_date = table.Column<DateOnly>(type: "date", nullable: false),
                    language_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safety_data_sheets", x => x.id);
                    table.ForeignKey(
                        name: "FK_safety_data_sheets_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_safety_data_sheets_product_id",
                table: "safety_data_sheets",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "safety_data_sheets");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
