namespace CafeMenu.Api.Services;

using CafeMenu.Api.Models;
using GiftShop.Model;
using System.Net.Http.Json;

public class ZarinPalService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _merchantId;
    private readonly ILogger<ZarinPalService> _logger;

    public ZarinPalService(HttpClient httpClient, IConfiguration config, ILogger<ZarinPalService> logger)
    {
        _httpClient = httpClient;
        _merchantId = config["ZarinPal:MerchantId"] ?? throw new ArgumentNullException("ZarinPal:MerchantId is missing");
        _logger = logger;
    }

    public async Task<string?> CreateRequestAsync(int amount, string callbackUrl, string description)
    {
        var request = new
        {
            merchant_id = _merchantId,
            amount,
            callback_url = callbackUrl,
            description
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("pg/v4/payment/request.json", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Zarinpal request failed with status {Status}. Body: {Body}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ZarinPalResponse>();

            if (result?.Data?.Code != 100)
            {
                _logger.LogError("Zarinpal request rejected: {Message}", result?.Data?.Message);
                return null;
            }

            return result.Data.Authority;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Catastrophic failure communicating with ZarinPal while creating a payment request.");
            return null;
        }
    }

    public async Task<PaymentVerificationResult> VerifyRequestAsync(string authority, int amount)
    {
        var request = new { merchant_id = _merchantId, authority, amount };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("pg/v4/payment/verify.json", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Zarinpal verify request failed with status {Status}. Body: {Body}", response.StatusCode, error);
                return new PaymentVerificationResult(false, null, "Gateway error during verification.");
            }

            var result = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponse>();

            if (result?.Data?.Code == 100 || result?.Data?.Code == 101)
            {
                _logger.LogInformation("Zarinpal verification success. RefId: {RefId}", result.Data.RefId);
                return new PaymentVerificationResult(true, result.Data.RefId, "Payment verified.");
            }

            _logger.LogWarning("Zarinpal verification failed: {Code} - {Message}", result?.Data?.Code, result?.Data?.Message);
            return new PaymentVerificationResult(false, null, result?.Data?.Message ?? "Payment verification failed.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to verify ZarinPal payment.");
            return new PaymentVerificationResult(false, null, "Unexpected payment verification failure.");
        }
    }
}