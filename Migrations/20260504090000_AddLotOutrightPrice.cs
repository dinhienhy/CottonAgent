using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CBAS.Web.Migrations
{
    public partial class AddLotOutrightPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OutrightPrice",
                table: "Lots",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OutrightPrice", table: "Lots");
        }
    }
}
