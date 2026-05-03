using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CBAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HVIReports",
                columns: table => new
                {
                    HVIId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LotCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Micronaire = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Length = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    StrengthGPT = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Uniformity = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ColorRd = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ColorGrade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Leaf = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CropYear = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TotalBales = table.Column<int>(type: "integer", nullable: true),
                    RawDataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HVIReports", x => x.HVIId);
                    table.UniqueConstraint("AK_HVIReports_LotCode", x => x.LotCode);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    OfferId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ICEValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.OfferId);
                });

            migrationBuilder.CreateTable(
                name: "OfferLots",
                columns: table => new
                {
                    LotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    LotCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CropYear = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpecialSpec = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BasisPoints = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PriceCentsPerLb = table.Column<decimal>(type: "numeric(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferLots", x => x.LotId);
                    table.ForeignKey(
                        name: "FK_OfferLots_HVIReports_LotCode",
                        column: x => x.LotCode,
                        principalTable: "HVIReports",
                        principalColumn: "LotCode",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OfferLots_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "OfferId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedOutputs",
                columns: table => new
                {
                    OutputId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    LotId = table.Column<int>(type: "integer", nullable: false),
                    STT = table.Column<int>(type: "integer", nullable: false),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CropYear = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpecialSpec = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Leaf = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Length = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Micronaire = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    StrengthMin = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Basis = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PriceCentsPerKg = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    PriceWithCommission = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    NetPrice = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedOutputs", x => x.OutputId);
                    table.ForeignKey(
                        name: "FK_ProcessedOutputs_OfferLots_LotId",
                        column: x => x.LotId,
                        principalTable: "OfferLots",
                        principalColumn: "LotId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessedOutputs_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "OfferId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HVIReports_LotCode",
                table: "HVIReports",
                column: "LotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferLots_LotCode",
                table: "OfferLots",
                column: "LotCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLots_OfferId",
                table: "OfferLots",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedOutputs_LotId",
                table: "ProcessedOutputs",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedOutputs_OfferId",
                table: "ProcessedOutputs",
                column: "OfferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedOutputs");

            migrationBuilder.DropTable(
                name: "OfferLots");

            migrationBuilder.DropTable(
                name: "HVIReports");

            migrationBuilder.DropTable(
                name: "Offers");
        }
    }
}
