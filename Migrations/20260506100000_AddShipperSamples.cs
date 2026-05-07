using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CBAS.Web.Migrations
{
    public partial class AddShipperSamples : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipperSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShipperId = table.Column<int>(type: "integer", nullable: false),
                    SampleOfferPdf = table.Column<byte[]>(type: "bytea", nullable: false),
                    SampleOfferFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SampleExcelResult = table.Column<byte[]>(type: "bytea", nullable: false),
                    SampleExcelFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExtractedPdfText = table.Column<string>(type: "text", nullable: true),
                    ExtractedExcelJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipperSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipperSamples_Shippers_ShipperId",
                        column: x => x.ShipperId,
                        principalTable: "Shippers",
                        principalColumn: "ShipperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipperSamples_ShipperId",
                table: "ShipperSamples",
                column: "ShipperId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ShipperSamples");
        }
    }
}
