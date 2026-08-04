namespace CafeMenu.Api.Services;

using CafeMenu.Api.Models;
using GiftShop.Model;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

public class ZarinPalPaymentService : IPaymentService
{
    private readonly HttpClient _http;
    private readonly PaymentOptions _options;
    private readonly ILogger<ZarinPalPaymentService> _logger;

    public bool IsSandbox => _options.UseSandbox;

    private string RequestUrl => IsSandbox
        ? "https://sandbox.zarinpal.com/pg/v4/payment/request.json"
        : "https://payment.zarinpal.com/pg/v4/payment/request.json";

    private string VerifyUrl => IsSandbox
        ? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json"
        : "https://payment.zarinpal.com/pg/v4/payment/verify.json";

    public ZarinPalPaymentService(HttpClient http, IOptions<PaymentOptions> options, ILogger<ZarinPalPaymentService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> CreateRequestAsync(int amount, string callbackUrl, string description)
    {
        var payload = new
        {
            merchant_id = _options.ZarinPal.MerchantId,
            amount,
            callback_url = callbackUrl,
            description,
            // NOTE: confirm whether `amount` here is already in Rial or Toman before relying on
            // sandbox results — v4 accepts a currency hint, but if your plan prices are stored in
            // Toman you may need "IRT" here (or multiply amount by 10) to match what ZarinPal expects.
            currency = "IRT",
        };

        try
        {
            var response = await _http.PostAsJsonAsync(RequestUrl, payload);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                var code = data.TryGetProperty("code", out var codeEl) ? codeEl.GetInt32() : 0;
                if (code == 100 && data.TryGetProperty("authority", out var authEl))
                {
                    return authEl.GetString();
                }
            }

            _logger.LogWarning(
                "ZarinPal payment request failed: {Error} (sandbox={Sandbox})",
                ExtractErrorMessage(root), IsSandbox);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal payment request threw (sandbox={Sandbox})", IsSandbox);
            return null;
        }
    }

    public async Task<ZarinPalVerificationResult> VerifyRequestAsync(string authority, int amount)
    {
        var payload = new
        {
            merchant_id = _options.ZarinPal.MerchantId,
            amount,
            authority,
        };

        try
        {
            var response = await _http.PostAsJsonAsync(VerifyUrl, payload);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                var code = data.TryGetProperty("code", out var codeEl) ? codeEl.GetInt32() : 0;

                // 100 = verified just now, 101 = this authority was already verified earlier —
                // ZarinPal treats both as a successful, idempotent verification.
                if (code is 100 or 101)
                {
                    long? refId = data.TryGetProperty("ref_id", out var refEl) && refEl.ValueKind == JsonValueKind.Number
                        ? refEl.GetInt64()
                        : null;

                    return new ZarinPalVerificationResult { IsSuccess = true, RefId = refId };
                }
            }

            return new ZarinPalVerificationResult
            {
                IsSuccess = false,
                ErrorMessage = ExtractErrorMessage(root),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal verification threw (sandbox={Sandbox})", IsSandbox);
            return new ZarinPalVerificationResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    // ZarinPal's "errors" field is [] on success and an object on failure — it genuinely changes
    // JSON type, so this has to branch on ValueKind instead of deserializing into a fixed model.
    private static string ExtractErrorMessage(JsonElement root)
    {
        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            var message = errors.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
            var code = errors.TryGetProperty("code", out var codeEl) ? codeEl.ToString() : null;
            return $"{message} (code: {code})";
        }
        return "Unknown ZarinPal error";
    }
}