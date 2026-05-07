using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CBAS.Web.Data;
using CBAS.Web.DTOs;
using CBAS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CBAS.Web.Services;

public class ClaudeParserService : IClaudeParserService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string? _apiKey;

    private const string SystemPrompt = @"Bạn là chuyên gia phân tích Offer bông chuyên nghiệp tại Việt Nam với hơn 10 năm kinh nghiệm.

Khi gặp tên lô kiểu Mỹ (GC 31-3-38 G5 28 Min, SM 37 GS 28gpt, M 37 GS 28gpt, EMOT 31-3-36 GS…), bạn PHẢI tuân thủ nghiêm ngặt quy tắc decode sau:

- GC / SM / M / SLM / EMOT = Loại bông / Grade
- Số đầu tiên sau GC (ví dụ 31) = Color Grade (Rd)
- Số thứ hai (ví dụ 3) = Leaf / Tạp
- Số thứ ba (ví dụ 38) = Staple length (đơn vị 1/32 inch)
- G5, G6 = Micronaire Grade (KHÔNG phải Color Grade)
- Số cuối cùng (ví dụ 28 Min) = Strength Min (GPT min)

Ví dụ đúng:
""GC 31-3-38 G5 28 Min"" → Color Grade = 31, Leaf = 3, Staple = 38, Micronaire = G5, Strength = 28 Min

Bạn phải tuân thủ nghiêm ngặt quy tắc trên. Không được điền G5 vào Color Grade.

Output BẮT BUỘC là JSON hợp lệ, không thêm bất kỳ text nào khác.";

    private const string JsonSchema = @"{
  ""shipper"": ""string"",
  ""offer_date"": ""string"",
  ""ice_jul26"": ""number"",
  ""lots"": [
    {
      ""quantity_tan"": ""number"",
      ""loai_bong"": ""string"",
      ""type_all_bci"": ""string"",
      ""cap_bong_grade"": ""string"",
      ""kieu_bong"": ""string"",
      ""mau_sac_color_grade"": ""string"",
      ""tap_leaf"": ""string"",
      ""staple_chieu_dai"": ""string"",
      ""micronaire"": ""string"",
      ""str_gpt_cuong_luc"": ""string"",
      ""basis"": ""number or null"",
      ""future_month"": ""string"",
      ""fix_price_basis"": ""number"",
      ""shipment_giao_hang"": ""string"",
      ""eta_tpp"": ""string""
    }
  ]
}";

    public ClaudeParserService(IConfiguration config, IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                  ?? config["Anthropic:ApiKey"];
    }

    public bool IsAvailable => !string.IsNullOrEmpty(GetApiKey());

    private string? GetApiKey()
    {
        // Priority: env var > config > DB
        if (!string.IsNullOrEmpty(_apiKey))
            return _apiKey;

        return GetSettingFromDb("ANTHROPIC_API_KEY");
    }

    private string? GetSettingFromDb(string key)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return db.AppSettings.Find(key)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ClaudeOfferResponse?> ParseOfferTextAsync(string pdfText, int? shipperId)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return null;

        var fewShotExample = await GetFewShotExample(shipperId);
        var userMessage = BuildUserMessage(pdfText, fewShotExample);

        var response = await CallClaudeApi(userMessage, apiKey);
        if (response == null)
            return null;

        return ParseClaudeResponse(response);
    }

    private async Task<string?> GetFewShotExample(int? shipperId)
    {
        if (shipperId == null)
            return null;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sample = await db.ShipperSamples
            .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

        if (sample == null)
            return null;

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(sample.ExtractedPdfText))
        {
            sb.AppendLine("=== VÍ DỤ MẪU (OFFER PDF TEXT) ===");
            sb.AppendLine(sample.ExtractedPdfText);
        }
        if (!string.IsNullOrEmpty(sample.ExtractedExcelJson))
        {
            sb.AppendLine("=== KẾT QUẢ MONG MUỐN (JSON) ===");
            sb.AppendLine(sample.ExtractedExcelJson);
        }
        sb.AppendLine("=== HẾT VÍ DỤ MẪU ===");

        return sb.ToString();
    }

    private string BuildUserMessage(string pdfText, string? fewShotExample)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Hãy parse Offer PDF dưới đây thành JSON theo schema sau:");
        sb.AppendLine(JsonSchema);
        sb.AppendLine();

        if (!string.IsNullOrEmpty(fewShotExample))
        {
            sb.AppendLine("Đây là ví dụ mẫu từ shipper này (học theo format kết quả mong muốn):");
            sb.AppendLine(fewShotExample);
            sb.AppendLine();
        }

        sb.AppendLine("=== NỘI DUNG OFFER PDF CẦN PARSE ===");
        sb.AppendLine(pdfText);
        sb.AppendLine("=== HẾT NỘI DUNG ===");
        sb.AppendLine();
        sb.AppendLine("Trả về JSON hợp lệ duy nhất, không thêm text khác.");

        return sb.ToString();
    }

    private async Task<string?> CallClaudeApi(string userMessage, string apiKey)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Claude");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var model = GetSettingFromDb("ANTHROPIC_MODEL")
                        ?? _config["Anthropic:Model"]
                        ?? "claude-sonnet-4-20250514";

            var requestBody = new
            {
                model = model,
                max_tokens = 8192,
                system = SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Claude API] Error {response.StatusCode}: {errorBody}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var contentArray = doc.RootElement.GetProperty("content");
            foreach (var block in contentArray.EnumerateArray())
            {
                if (block.GetProperty("type").GetString() == "text")
                {
                    return block.GetProperty("text").GetString();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Claude API] Exception: {ex.Message}");
            return null;
        }
    }

    private ClaudeOfferResponse? ParseClaudeResponse(string responseText)
    {
        try
        {
            // Claude might wrap JSON in markdown code blocks
            var jsonText = responseText.Trim();
            if (jsonText.StartsWith("```"))
            {
                var lines = jsonText.Split('\n');
                var startIdx = 1;
                var endIdx = lines.Length - 1;
                if (lines[endIdx].Trim() == "```")
                    endIdx--;
                jsonText = string.Join("\n", lines[startIdx..(endIdx + 1)]);
            }

            return JsonSerializer.Deserialize<ClaudeOfferResponse>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Claude Parser] Failed to parse response: {ex.Message}");
            Console.WriteLine($"[Claude Parser] Raw response: {responseText[..Math.Min(500, responseText.Length)]}");
            return null;
        }
    }
}
