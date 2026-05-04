using CBAS.Web.Models;

namespace CBAS.Web.Services.Parsers;

public interface IShipperParser
{
    string ShipperName { get; }
    bool CanParse(string fileName, List<string> pdfRows);
    OfferParseResult Parse(List<string> rows, int offerId);
}
