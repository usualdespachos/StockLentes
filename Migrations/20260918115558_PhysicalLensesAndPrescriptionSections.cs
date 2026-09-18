using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLentes.Migrations
{
    /// <inheritdoc />
    public partial class PhysicalLensesAndPrescriptionSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionGroupId",
                table: "OrderLenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresUsedPrescription",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsedAdd100",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsedAxis",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsedCylinder100",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsedSphere100",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsesActualGraduation",
                table: "OrderLenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PrescriptionSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderLensId = table.Column<int>(type: "INTEGER", nullable: false),
                    Section = table.Column<string>(type: "TEXT", nullable: false),
                    Sphere = table.Column<string>(type: "TEXT", nullable: true),
                    Cylinder = table.Column<string>(type: "TEXT", nullable: true),
                    Axis = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionSections_OrderLenses_OrderLensId",
                        column: x => x.OrderLensId,
                        principalTable: "OrderLenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLenses_LensOrderId_PrescriptionGroupId_Eye",
                table: "OrderLenses",
                columns: new[] { "LensOrderId", "PrescriptionGroupId", "Eye" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionSections_OrderLensId_Section",
                table: "PrescriptionSections",
                columns: new[] { "OrderLensId", "Section" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionSections");

            migrationBuilder.DropIndex(
                name: "IX_OrderLenses_LensOrderId_PrescriptionGroupId_Eye",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "PrescriptionGroupId",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "RequiresUsedPrescription",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "UsedAdd100",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "UsedAxis",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "UsedCylinder100",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "UsedSphere100",
                table: "OrderLenses");

            migrationBuilder.DropColumn(
                name: "UsesActualGraduation",
                table: "OrderLenses");
        }
    }
}
