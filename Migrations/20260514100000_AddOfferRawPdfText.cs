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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawPdfText",
                table: "Offers");
        }
    }
}
