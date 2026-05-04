using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CBAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsForRealOfferFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Offer: Add ICESettlementsJson
            migrationBuilder.AddColumn<string>(
                name: "ICESettlementsJson",
                table: "Offers",
                type: "text",
                nullable: true);

            // OfferLot: Make LotCode nullable
            migrationBuilder.AlterColumn<string>(
                name: "LotCode",
                table: "OfferLots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // OfferLot: Rename BasisPoints to BasisCents
            migrationBuilder.RenameColumn(
                name: "BasisPoints",
                table: "OfferLots",
                newName: "BasisCents");

            // OfferLot: Add new columns
            migrationBuilder.AddColumn<string>(
                name: "QuantityText",
                table: "OfferLots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutrightPrice",
                table: "OfferLots",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SettlementMonth",
                table: "OfferLots",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentDateText",
                table: "OfferLots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorSpec",
                table: "OfferLots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeafSpec",
                table: "OfferLots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LengthSpec",
                table: "OfferLots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicronaireSpec",
                table: "OfferLots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrengthSpec",
                table: "OfferLots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // ProcessedOutput: Add new display columns
            migrationBuilder.AddColumn<string>(
                name: "QuantityText",
                table: "ProcessedOutputs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicronaireText",
                table: "ProcessedOutputs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrengthText",
                table: "ProcessedOutputs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentDateText",
                table: "ProcessedOutputs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ProcessedOutput: Remove new columns
            migrationBuilder.DropColumn(name: "QuantityText", table: "ProcessedOutputs");
            migrationBuilder.DropColumn(name: "MicronaireText", table: "ProcessedOutputs");
            migrationBuilder.DropColumn(name: "StrengthText", table: "ProcessedOutputs");
            migrationBuilder.DropColumn(name: "ShipmentDateText", table: "ProcessedOutputs");

            // OfferLot: Remove new columns
            migrationBuilder.DropColumn(name: "QuantityText", table: "OfferLots");
            migrationBuilder.DropColumn(name: "OutrightPrice", table: "OfferLots");
            migrationBuilder.DropColumn(name: "SettlementMonth", table: "OfferLots");
            migrationBuilder.DropColumn(name: "ShipmentDateText", table: "OfferLots");
            migrationBuilder.DropColumn(name: "ColorSpec", table: "OfferLots");
            migrationBuilder.DropColumn(name: "LeafSpec", table: "OfferLots");
            migrationBuilder.DropColumn(name: "LengthSpec", table: "OfferLots");
            migrationBuilder.DropColumn(name: "MicronaireSpec", table: "OfferLots");
            migrationBuilder.DropColumn(name: "StrengthSpec", table: "OfferLots");

            // OfferLot: Rename BasisCents back to BasisPoints
            migrationBuilder.RenameColumn(
                name: "BasisCents",
                table: "OfferLots",
                newName: "BasisPoints");

            // OfferLot: Make LotCode required again
            migrationBuilder.AlterColumn<string>(
                name: "LotCode",
                table: "OfferLots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Offer: Remove ICESettlementsJson
            migrationBuilder.DropColumn(name: "ICESettlementsJson", table: "Offers");
        }
    }
}
