using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CBAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOfferLotHVIForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfferLots_HVIReports_LotCode",
                table: "OfferLots");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_HVIReports_LotCode",
                table: "HVIReports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_HVIReports_LotCode",
                table: "HVIReports",
                column: "LotCode");

            migrationBuilder.AddForeignKey(
                name: "FK_OfferLots_HVIReports_LotCode",
                table: "OfferLots",
                column: "LotCode",
                principalTable: "HVIReports",
                principalColumn: "LotCode",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
