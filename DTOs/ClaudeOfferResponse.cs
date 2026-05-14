using System.Text.Json.Serialization;

namespace CBAS.Web.DTOs;

public class ClaudeOfferResponse
{
    [JsonPropertyName("shipper")]
    public string Shipper { get; set; } = string.Empty;

    [JsonPropertyName("offer_date")]
    public string? OfferDate { get; set; }

    [JsonPropertyName("ice_jul26")]
    public decimal? IceJul26 { get; set; }

    [JsonPropertyName("lots")]
    public List<ClaudeOfferLot> Lots { get; set; } = new();
}

public class ClaudeOfferLot
{
    [JsonPropertyName("quantity_tan")]
    public decimal QuantityTan { get; set; }

    [JsonPropertyName("loai_bong")]
    public string? LoaiBong { get; set; }

    [JsonPropertyName("type_all_bci")]
    public string? TypeAllBci { get; set; }

    [JsonPropertyName("cap_bong_grade")]
    public string? CapBongGrade { get; set; }

    [JsonPropertyName("kieu_bong")]
    public string? KieuBong { get; set; }

    [JsonPropertyName("mau_sac_color_grade")]
    public string? MauSacColorGrade { get; set; }

    [JsonPropertyName("tap_leaf")]
    public string? TapLeaf { get; set; }

    [JsonPropertyName("staple_chieu_dai")]
    public string? StapleChieuDai { get; set; }

    [JsonPropertyName("micronaire")]
    public string? Micronaire { get; set; }

    [JsonPropertyName("str_gpt_cuong_luc")]
    public string? StrGptCuongLuc { get; set; }

    [JsonPropertyName("crop_year")]
    public string? CropYear { get; set; }

    [JsonPropertyName("basis")]
    public decimal? Basis { get; set; }

    [JsonPropertyName("future_month")]
    public string? FutureMonth { get; set; }

    [JsonPropertyName("fix_price_basis")]
    public decimal? FixPriceBasis { get; set; }

    [JsonPropertyName("shipment_giao_hang")]
    public string? ShipmentGiaoHang { get; set; }

    [JsonPropertyName("eta_tpp")]
    public string? EtaTpp { get; set; }
}
