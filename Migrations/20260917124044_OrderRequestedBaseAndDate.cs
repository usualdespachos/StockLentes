using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLentes.Migrations
{
    /// <inheritdoc />
    public partial class OrderRequestedBaseAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "OrderDate",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
            migrationBuilder.Sql("UPDATE Orders SET OrderDate = substr(CreatedAtUtc, 1, 10)");

            migrationBuilder.AddColumn<int>(
                name: "Base100",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Base100",
                table: "OrderLenses");
        }
    }
}
