using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GiftShop.Model
{
    public class ZarinPalVerifyData
    {
        [JsonPropertyName("code")]
        public long Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("card_hash")]
        public string CardHash { get; set; } = string.Empty;

        [JsonPropertyName("card_pan")]
        public string CardPan { get; set; } = string.Empty;

        [JsonPropertyName("ref_id")]
        public long RefId { get; set; }

        [JsonPropertyName("fee_type")]
        public string FeeType { get; set; } = string.Empty;

        [JsonPropertyName("fee")]
        public long Fee { get; set; }
    }

    public class ZarinPalVerifyResponse
    {
        [JsonPropertyName("data")]
        public ZarinPalVerifyData Data { get; set; } = new ZarinPalVerifyData();

        [JsonPropertyName("errors")]
        public List<object> Errors { get; set; } = new List<object>();
    }
}
