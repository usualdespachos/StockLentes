using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLentes.Migrations
{
    /// <inheritdoc />
    public partial class PartialOrdersAndAtomicCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderLenses_LensOrderId_Eye_PairType_PairNumber",
                table: "OrderLenses");

            migrationBuilder.AlterColumn<int>(
                name: "OpticalStoreId",
                table: "Orders",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "HeaderReviewReason",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalHeaderJson",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "InterpretationNote",
                table: "OrderLenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalInputJson",
                table: "OrderLenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RawInputJson",
                table: "OrderLenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SelectedBase100",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedProductId",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedStockId",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectionReason",
                table: "OrderLenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "OrderLenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OrderLenses_LensOrderId_Eye_PairType_PairNumber",
                table: "OrderLenses",
                columns: new[] { "LensOrderId", "Eye", "PairType", "PairNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderLenses_LensOrderId_Eye_PairType_PairNumber",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "HeaderReviewReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OriginalHeaderJson",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InterpretationNote",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "OriginalInputJson",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "RawInputJson",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "SelectedBase100",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "SelectedProductId",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "SelectedStockId",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "SelectionReason",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OrderLenses");

            migrationBuilder.AlterColumn<int>(
                name: "OpticalStoreId",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLenses_LensOrderId_Eye_PairType_PairNumber",
                table: "OrderLenses",
                columns: new[] { "LensOrderId", "Eye", "PairType", "PairNumber" },
                unique: true);
        }
    }
}
