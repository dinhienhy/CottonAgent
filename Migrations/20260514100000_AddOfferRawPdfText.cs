using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CBAS.Web.Migrations
{
    public partial class AddOfferRawPdfText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawPdfText",
                table: "Offers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLineNumber",
                table: "OfferLots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRawLine",
                table: "OfferLots",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawPdfText",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "SourceLineNumber",
                table: "OfferLots");

            migrationBuilder.DropColumn(
                name: "SourceRawLine",
                table: "OfferLots");
        }
    }
}
