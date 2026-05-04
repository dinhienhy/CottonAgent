using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CBAS.Web.Migrations
{
    public partial class AddShipperAndLot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Shippers table
            migrationBuilder.CreateTable(
                name: "Shippers",
                columns: table => new
                {
                    ShipperId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shippers", x => x.ShipperId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shippers_Name",
                table: "Shippers",
                column: "Name",
                unique: true);

            // Create Lots table
            migrationBuilder.CreateTable(
                name: "Lots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LotCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShipperId = table.Column<int>(type: "integer", nullable: false),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CropYear = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuantityOriginal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    QuantityAvailable = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LatestOfferId = table.Column<int>(type: "integer", nullable: true),
                    HVIReportId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lots_Shippers_ShipperId",
                        column: x => x.ShipperId,
                        principalTable: "Shippers",
                        principalColumn: "ShipperId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lots_Offers_LatestOfferId",
                        column: x => x.LatestOfferId,
                        principalTable: "Offers",
                        principalColumn: "OfferId");
                    table.ForeignKey(
                        name: "FK_Lots_HVIReports_HVIReportId",
                        column: x => x.HVIReportId,
                        principalTable: "HVIReports",
                        principalColumn: "HVIId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_LotCode",
                table: "Lots",
                column: "LotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ShipperId",
                table: "Lots",
                column: "ShipperId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_LatestOfferId",
                table: "Lots",
                column: "LatestOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_HVIReportId",
                table: "Lots",
                column: "HVIReportId");

            // Add ShipperId FK to Offers
            migrationBuilder.AddColumn<int>(
                name: "ShipperId",
                table: "Offers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ShipperId",
                table: "Offers",
                column: "ShipperId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Shippers_ShipperId",
                table: "Offers",
                column: "ShipperId",
                principalTable: "Shippers",
                principalColumn: "ShipperId");

            // Add MasterLotId FK to OfferLots
            migrationBuilder.AddColumn<int>(
                name: "MasterLotId",
                table: "OfferLots",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferLots_MasterLotId",
                table: "OfferLots",
                column: "MasterLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_OfferLots_Lots_MasterLotId",
                table: "OfferLots",
                column: "MasterLotId",
                principalTable: "Lots",
                principalColumn: "Id");

            // Add MasterLotId FK to HVIReports
            migrationBuilder.AddColumn<int>(
                name: "MasterLotId",
                table: "HVIReports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HVIReports_MasterLotId",
                table: "HVIReports",
                column: "MasterLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_HVIReports_Lots_MasterLotId",
                table: "HVIReports",
                column: "MasterLotId",
                principalTable: "Lots",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_HVIReports_Lots_MasterLotId", table: "HVIReports");
            migrationBuilder.DropIndex(name: "IX_HVIReports_MasterLotId", table: "HVIReports");
            migrationBuilder.DropColumn(name: "MasterLotId", table: "HVIReports");

            migrationBuilder.DropForeignKey(name: "FK_OfferLots_Lots_MasterLotId", table: "OfferLots");
            migrationBuilder.DropIndex(name: "IX_OfferLots_MasterLotId", table: "OfferLots");
            migrationBuilder.DropColumn(name: "MasterLotId", table: "OfferLots");

            migrationBuilder.DropForeignKey(name: "FK_Offers_Shippers_ShipperId", table: "Offers");
            migrationBuilder.DropIndex(name: "IX_Offers_ShipperId", table: "Offers");
            migrationBuilder.DropColumn(name: "ShipperId", table: "Offers");

            migrationBuilder.DropTable(name: "Lots");
            migrationBuilder.DropTable(name: "Shippers");
        }
    }
}
