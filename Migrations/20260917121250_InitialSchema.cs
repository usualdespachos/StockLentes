using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLentes.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpticalStores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDemo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpticalStores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UsesSphere = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesCylinder = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesAdd = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesBase = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManualBase = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpticalStoreId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalNumber = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_OpticalStores_OpticalStoreId",
                        column: x => x.OpticalStoreId,
                        principalTable: "OpticalStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LensFamilies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Sector = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StockRuleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LensFamilies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LensFamilies_StockRules_StockRuleId",
                        column: x => x.StockRuleId,
                        principalTable: "StockRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    LensFamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    TracksStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemo = table.Column<bool>(type: "INTEGER", nullable: false),
                    LowStockHalfPairs = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_LensFamilies_LensFamilyId",
                        column: x => x.LensFamilyId,
                        principalTable: "LensFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LensOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Eye = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PairType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PairNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedProductId = table.Column<int>(type: "INTEGER", nullable: true),
                    OriginalProductCode = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    OriginalProductName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Sphere100 = table.Column<int>(type: "INTEGER", nullable: true),
                    Cylinder100 = table.Column<int>(type: "INTEGER", nullable: true),
                    Axis = table.Column<int>(type: "INTEGER", nullable: true),
                    Add100 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLenses_Orders_LensOrderId",
                        column: x => x.LensOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLenses_Products_RequestedProductId",
                        column: x => x.RequestedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LensProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    CombinationKey = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Sphere100 = table.Column<int>(type: "INTEGER", nullable: true),
                    Cylinder100 = table.Column<int>(type: "INTEGER", nullable: true),
                    Add100 = table.Column<int>(type: "INTEGER", nullable: true),
                    Base100 = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantityHalfPairs = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock", x => x.Id);
                    table.CheckConstraint("CK_Stock_Nonnegative", "QuantityHalfPairs >= 0");
                    table.ForeignKey(
                        name: "FK_Stock_Products_LensProductId",
                        column: x => x.LensProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderLensId = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    StockBalanceId = table.Column<int>(type: "INTEGER", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    QuantityHalfPairs = table.Column<int>(type: "INTEGER", nullable: false),
                    StockBeforeHalfPairs = table.Column<int>(type: "INTEGER", nullable: true),
                    StockAfterHalfPairs = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualSphere100 = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualCylinder100 = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualAxis = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualAdd100 = table.Column<int>(type: "INTEGER", nullable: true),
                    SuggestedBase100 = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualBase100 = table.Column<int>(type: "INTEGER", nullable: true),
                    ManualSelection = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RequestedSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    ActualProductName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ActualProductCode = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movements_OrderLenses_OrderLensId",
                        column: x => x.OrderLensId,
                        principalTable: "OrderLenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movements_Products_ActualProductId",
                        column: x => x.ActualProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movements_Stock_StockBalanceId",
                        column: x => x.StockBalanceId,
                        principalTable: "Stock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LensFamilies_StockRuleId",
                table: "LensFamilies",
                column: "StockRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_ActualProductId",
                table: "Movements",
                column: "ActualProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_IdempotencyKey",
                table: "Movements",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movements_OrderLensId",
                table: "Movements",
                column: "OrderLensId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movements_StockBalanceId",
                table: "Movements",
                column: "StockBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLenses_LensOrderId_Eye_PairType_PairNumber",
                table: "OrderLenses",
                columns: new[] { "LensOrderId", "Eye", "PairType", "PairNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLenses_RequestedProductId",
                table: "OrderLenses",
                column: "RequestedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OpticalStoreId_ExternalNumber",
                table: "Orders",
                columns: new[] { "OpticalStoreId", "ExternalNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_LensFamilyId",
                table: "Products",
                column: "LensFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_LensProductId_CombinationKey",
                table: "Stock",
                columns: new[] { "LensProductId", "CombinationKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movements");

            migrationBuilder.DropTable(
                name: "OrderLenses");

            migrationBuilder.DropTable(
                name: "Stock");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "OpticalStores");

            migrationBuilder.DropTable(
                name: "LensFamilies");

            migrationBuilder.DropTable(
                name: "StockRules");
        }
    }
}
