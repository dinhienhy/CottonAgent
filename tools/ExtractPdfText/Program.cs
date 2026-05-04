using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

var sampleDir = @"e:\Dropbox\Workspace\CascadeProjects\CottonAgent\SampleData\Toyoshima";
var pdfFiles = Directory.GetFiles(sampleDir, "*.pdf");

foreach (var pdfFile in pdfFiles)
{
    Console.WriteLine($"\n{"=",-80}");
    Console.WriteLine($"FILE: {Path.GetFileName(pdfFile)}");
    Console.WriteLine($"{"=",-80}");

    try
    {
        using var document = PdfDocument.Open(pdfFile);
        Console.WriteLine($"Pages: {document.NumberOfPages}");

        for (int i = 0; i < document.NumberOfPages; i++)
        {
            var page = document.GetPage(i + 1);
            var words = page.GetWords().ToList();
            var letters = page.Letters.ToList();

            Console.WriteLine($"\n--- Page {i + 1}: {words.Count} words, {letters.Count} letters ---");

            if (words.Count == 0 && letters.Count == 0)
            {
                Console.WriteLine("NO TEXT CONTENT - likely scanned image PDF");
                continue;
            }

            // Group words by Y coordinate (row), tolerance 3px
            var rows = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .OrderByDescending(g => g.Key)
                .ToList();

            Console.WriteLine($"Detected {rows.Count} text rows:\n");

            int rowNum = 1;
            foreach (var row in rows)
            {
                var sortedWords = row.OrderBy(w => w.BoundingBox.Left).ToList();
                var rowText = string.Join(" ", sortedWords.Select(w => w.Text));
                Console.WriteLine($"  Row {rowNum,3} (Y={row.Key,6:F0}): {rowText}");
                rowNum++;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
}
