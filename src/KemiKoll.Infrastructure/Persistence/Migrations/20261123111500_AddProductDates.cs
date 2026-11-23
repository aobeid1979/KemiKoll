using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemiKoll.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds when a product was added and last changed.
    /// Products that already exist get the time of the migration (now()) for both.
    /// </summary>
    public partial class AddProductDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "added_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_changed_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            // The default was only needed to fill the existing rows; the application always sets both values.
            migrationBuilder.Sql("ALTER TABLE products ALTER COLUMN added_at DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE products ALTER COLUMN last_changed_at DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "added_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "last_changed_at",
                table: "products");
        }
    }
}
