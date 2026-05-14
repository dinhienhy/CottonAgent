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

Khi gặp tên lô kiểu Mỹ hoặc Mexico, bạn PHẢI tuân thủ nghiêm ngặt quy tắc decode sau:

**Quy tắc decode tên lô (bắt buộc):**
- GC / SM / M / SLM / EMOT = Loại bông / Grade
- Số đầu tiên sau GC (ví dụ 31 trong GC 31-3-38) = Color Grade (Rd)
- Số thứ hai (ví dụ 3) = Leaf / Tạp
- **Staple / Chiều dài (rất quan trọng):**
  - Nếu gặp ""1-1/8"", ""1-3/16"", ""1-5/32"", ""1-7/32"" → ghi nguyên dạng ""1-1/8""
  - Nếu gặp ""Stpl 35.55"", ""Stpl 33.83"", ""35.55"" → ghi số đó (35.55)
  - Nếu chỉ có số 35, 36, 37, 38 → ghi cả hai dạng nếu cần (ví dụ: 35 hoặc 1-1/8)
- G5, G6 = Micronaire Grade (KHÔNG được điền vào Color Grade)
- Số cuối cùng (28 Min, 27.20 Avg, 32 GPT…) = Strength (GPT)

**Crop Year:** Luôn trích xuất rõ ràng (25/26, 2025, 2026, 2025 Crop…)

**Basis & Giá:**
- Ưu tiên lấy Fix price nếu có.
- Nếu có On Call Jul'26 hoặc Basis → ghi số points (1850, 1100…).
- Không được bỏ sót Basis.

**Shipment / Giao hàng:** Chỉ dùng 4 giá trị: ""Origin"", ""Prompt"", ""Afloat"", ""Warehouse"".

Ví dụ đúng:
""GC 31-3-38 G5 28 Min"" → Color Grade = 31, Leaf = 3, Staple = 1-1/8, Micronaire = G5, Strength = 28 Min
""SM, Stpl 35.55, Mic 4.28 Avg, Gpt 27.20 Avg"" → Staple = 35.55, Strength = 27.20 Avg

Bạn phải tuân thủ nghiêm ngặt. Không được nhầm Mexico thành Mỹ, không được bỏ sót Crop Year, Basis, Staple.

Output BẮT BUỘC là JSON hợp lệ, không thêm bất kỳ text nào khác.
Bạn PHẢI trả về kết quả chính xác và nhất quán. Với cùng một file Offer, output JSON phải giống hệt nhau mỗi lần gọi. Không được bỏ sót lot nào, không được tự suy diễn thêm lot không có trong text.";

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
      ""crop_year"": ""string"",
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
        var (result, _) = await ParseOfferWithLogAsync(pdfText, shipperId);
        return result;
    }

    public async Task<(ClaudeOfferResponse? Result, ClaudeParseLog Log)> ParseOfferWithLogAsync(string pdfText, int? shipperId)
    {
        var log = new ClaudeParseLog { Used = true, Status = "processing" };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            log.Status = "error";
            log.ErrorMessage = "Không tìm thấy API Key (env/config/DB)";
            log.AddStep("API Key not found");
            return (null, log);
        }
        log.AddStep("API Key OK");

        var model = GetSettingFromDb("ANTHROPIC_MODEL")
                    ?? _config["Anthropic:Model"]
                    ?? "claude-3-5-sonnet-20241022";
        log.Model = model;
        log.AddStep($"Model: {model}");

        var fewShotExample = await GetFewShotExample(shipperId);
        log.HasFewShot = !string.IsNullOrEmpty(fewShotExample);
        log.AddStep(log.HasFewShot ? "Few-shot sample found" : "No few-shot sample (new shipper)");

        var userMessage = BuildUserMessage(pdfText);
        log.AddStep($"Prompt built (user={userMessage.Length:N0} chars). Calling Claude API (temperature=0, prompt caching)...");

        try
        {
            var client = _httpClientFactory.CreateClient("Claude");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.DefaultRequestHeaders.Add("anthropic-beta", "prompt-caching-2024-07-31");

            // Build system blocks with cache_control
            var systemBlocks = BuildSystemBlocks(fewShotExample);

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["max_tokens"] = 32000,
                ["temperature"] = 0,
                ["system"] = systemBlocks,
                ["messages"] = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                log.Status = "error";
                log.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.StatusCode}";
                log.RawResponse = responseJson.Length > 2000 ? responseJson[..2000] : responseJson;
                log.AddStep($"API Error: {log.ErrorMessage}");
                sw.Stop();
                log.ElapsedSeconds = sw.Elapsed.TotalSeconds;
                return (null, log);
            }

            log.AddStep("API response received");

            // Parse response metadata
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("usage", out var usage))
            {
                log.InputTokens = usage.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : null;
                log.OutputTokens = usage.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : null;
                log.CacheCreationTokens = usage.TryGetProperty("cache_creation_input_tokens", out var cc) ? cc.GetInt32() : null;
                log.CacheReadTokens = usage.TryGetProperty("cache_read_input_tokens", out var cr) ? cr.GetInt32() : null;
            }
            if (root.TryGetProperty("stop_reason", out var sr))
            {
                log.StopReason = sr.GetString();
            }

            var cacheInfo = log.CacheReadTokens > 0 ? $", cache_read={log.CacheReadTokens}" 
                         : log.CacheCreationTokens > 0 ? $", cache_created={log.CacheCreationTokens}" 
                         : "";
            log.AddStep($"Tokens: in={log.InputTokens}, out={log.OutputTokens}, stop={log.StopReason}{cacheInfo}");

            // Extract text content
            string? textContent = null;
            var contentArray = root.GetProperty("content");
            foreach (var block in contentArray.EnumerateArray())
            {
                if (block.GetProperty("type").GetString() == "text")
                {
                    textContent = block.GetProperty("text").GetString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(textContent))
            {
                log.Status = "error";
                log.ErrorMessage = "No text content in response";
                log.RawResponse = responseJson.Length > 2000 ? responseJson[..2000] : responseJson;
                log.AddStep("ERROR: No text content block");
                sw.Stop();
                log.ElapsedSeconds = sw.Elapsed.TotalSeconds;
                return (null, log);
            }

            log.RawResponse = textContent.Length > 3000 ? textContent[..3000] + "\n...[truncated]" : textContent;

            // If truncated, try to repair JSON by closing brackets
            if (log.StopReason == "max_tokens")
            {
                log.AddStep("WARNING: Response bị cắt do hết max_tokens! Đang thử repair JSON...");
                textContent = TryRepairTruncatedJson(textContent);
            }

            // Parse JSON result
            var parsed = ParseClaudeResponse(textContent);
            if (parsed == null && log.StopReason == "max_tokens")
            {
                log.Status = "error";
                log.ErrorMessage = "JSON bị cắt (max_tokens) và không thể repair → fallback regex";
                log.AddStep("ERROR: Truncated JSON unrecoverable → will fallback to regex parser");
                sw.Stop();
                log.ElapsedSeconds = sw.Elapsed.TotalSeconds;
                return (null, log);
            }
            if (parsed == null)
            {
                log.Status = "error";
                log.ErrorMessage = "JSON parse failed";
                log.AddStep("ERROR: Cannot parse Claude response as JSON");
                sw.Stop();
                log.ElapsedSeconds = sw.Elapsed.TotalSeconds;
                return (null, log);
            }

            log.LotsFound = parsed.Lots.Count;
            log.Status = parsed.Lots.Count > 0 ? "success" : "error";
            if (log.StopReason == "max_tokens")
                log.AddStep($"Parsed OK (repaired truncated JSON): {parsed.Lots.Count} lots — có thể thiếu lots cuối");
            else
                log.AddStep($"Parsed OK: {parsed.Lots.Count} lots, shipper={parsed.Shipper}");
            sw.Stop();
            log.ElapsedSeconds = sw.Elapsed.TotalSeconds;
            return (parsed, log);
        }
        catch (Exception ex)
        {
            log.Status = "error";
            log.ErrorMessage = ex.Message;
            log.AddStep($"EXCEPTION: {ex.Message}");
            sw.Stop();
            log.ElapsedSeconds = sw.Elapsed.TotalSeconds;
            return (null, log);
        }
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

    private List<Dictionary<string, object>> BuildSystemBlocks(string? fewShotExample)
    {
        var blocks = new List<Dictionary<string, object>>();

        // Block 1: System prompt + JSON schema (cached)
        var systemText = SystemPrompt + "\n\nJSON Schema bắt buộc:\n" + JsonSchema;
        blocks.Add(new Dictionary<string, object>
        {
            ["type"] = "text",
            ["text"] = systemText,
            ["cache_control"] = new { type = "ephemeral" }
        });

        // Block 2: Few-shot example (cached, if available)
        if (!string.IsNullOrEmpty(fewShotExample))
        {
            blocks.Add(new Dictionary<string, object>
            {
                ["type"] = "text",
                ["text"] = "Đây là ví dụ mẫu từ shipper này (học theo format kết quả mong muốn):\n" + fewShotExample,
                ["cache_control"] = new { type = "ephemeral" }
            });
        }

        return blocks;
    }

    private string BuildUserMessage(string pdfText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== NỘI DUNG OFFER PDF CẦN PARSE ===");
        sb.AppendLine(pdfText);
        sb.AppendLine("=== HẾT NỘI DUNG ===");
        sb.AppendLine();
        sb.AppendLine("Trả về JSON compact trên 1 dòng (không indent, không xuống dòng), không markdown, không text khác.");
        return sb.ToString();
    }


    private static string TryRepairTruncatedJson(string json)
    {
        // Find last complete lot object (ends with })
        var lastCloseBrace = json.LastIndexOf('}');
        if (lastCloseBrace < 0) return json;

        // Trim to last complete object, then close the array and root object
        var trimmed = json[..(lastCloseBrace + 1)];

        // Count unclosed brackets
        int braces = 0, brackets = 0;
        foreach (var c in trimmed)
        {
            if (c == '{') braces++;
            else if (c == '}') braces--;
            else if (c == '[') brackets++;
            else if (c == ']') brackets--;
        }

        var sb = new StringBuilder(trimmed);
        while (braces > 0) { sb.Append('}'); braces--; }
        while (brackets > 0) { sb.Append(']'); brackets--; }

        Console.WriteLine($"[Claude Parser] Repaired truncated JSON: added {braces} braces, {brackets} brackets");
        return sb.ToString();
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
