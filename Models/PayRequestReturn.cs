using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GiftShop.Model
{
    public class ZarinPalData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("authority")]
        public string Authority { get; set; } = string.Empty;

        [JsonPropertyName("fee_type")]
        public string FeeType { get; set; } = string.Empty;

        [JsonPropertyName("fee")]
        public int Fee { get; set; }
    }

    public class ZarinPalResponse
    {
        [JsonPropertyName("data")]
        public ZarinPalData Data { get; set; } = new ZarinPalData();

        [JsonPropertyName("errors")]
        public List<object> Errors { get; set; } = new List<object>();
    }
}

