using CBAS.Web.DTOs;
using ClosedXML.Excel;

namespace CBAS.Web.Services;

public class ExcelExportService : IExcelExportService
{
    public async Task<byte[]> GenerateExcelAsync(List<OutputGroupDto> groups)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Cotton Offer");

            var headers = new[]
            {
                "STT", "origin", "crop year - vụ mùa", "số lượng", "loại bông",
                "chỉ tiêu đặc biệt", "Màu sắc", "tạp lá", "chiều dài", "Micronaire",
                "cường lực Min", "Basis", "Shipment Date", "giá (c/kg)",
                "giá có Commission", "giá net (Toyoshima)", "Ghi chú"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            int currentRow = 2;

            foreach (var group in groups)
            {
                var groupTitleCell = worksheet.Cell(currentRow, 1);
                groupTitleCell.Value = group.GroupTitle;
                groupTitleCell.Style.Font.Bold = true;
                groupTitleCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                
                var groupRange = worksheet.Range(currentRow, 1, currentRow, headers.Length);
                groupRange.Merge();
                groupRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                
                currentRow++;

                foreach (var row in group.Rows)
                {
                    worksheet.Cell(currentRow, 1).Value = row.STT;
                    worksheet.Cell(currentRow, 2).Value = row.Origin;
                    worksheet.Cell(currentRow, 3).Value = row.CropYear;
                    worksheet.Cell(currentRow, 4).Value = row.Quantity;
                    worksheet.Cell(currentRow, 5).Value = row.Type;
                    worksheet.Cell(currentRow, 6).Value = row.SpecialSpec ?? "";
                    worksheet.Cell(currentRow, 7).Value = row.Color ?? "";
                    worksheet.Cell(currentRow, 8).Value = row.Leaf.HasValue ? (double)row.Leaf.Value : "";
                    worksheet.Cell(currentRow, 9).Value = row.Length.HasValue ? (double)row.Length.Value : "";
                    worksheet.Cell(currentRow, 10).Value = row.Micronaire.HasValue ? (double)row.Micronaire.Value : "";
                    worksheet.Cell(currentRow, 11).Value = row.StrengthMin.HasValue ? (double)row.StrengthMin.Value : "";
                    worksheet.Cell(currentRow, 12).Value = (double)row.Basis;
                    worksheet.Cell(currentRow, 13).Value = row.ShipmentDate?.ToString("dd/MM/yyyy") ?? "";
                    worksheet.Cell(currentRow, 14).Value = (double)row.PriceCentsPerKg;
                    worksheet.Cell(currentRow, 15).Value = (double)row.PriceWithCommission;
                    worksheet.Cell(currentRow, 16).Value = (double)row.NetPrice;
                    worksheet.Cell(currentRow, 17).Value = row.Notes ?? "";

                    currentRow++;
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        });
    }
}
