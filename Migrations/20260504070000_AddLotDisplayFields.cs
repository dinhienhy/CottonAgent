using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CBAS.Web.Migrations
{
    public partial class AddLotDisplayFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasisCents",
                table: "Lots",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentDate",
                table: "Lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentDateText",
                table: "Lots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialSpec",
                table: "Lots",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BasisCents", table: "Lots");
            migrationBuilder.DropColumn(name: "ShipmentDate", table: "Lots");
            migrationBuilder.DropColumn(name: "ShipmentDateText", table: "Lots");
            migrationBuilder.DropColumn(name: "SpecialSpec", table: "Lots");
        }
    }
}
