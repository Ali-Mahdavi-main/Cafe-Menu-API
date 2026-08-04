
public class PaymentOptions
{
    public bool UseSandbox { get; set; }
    public ZarinPalOptions ZarinPal { get; set; } = new();
}

public class ZarinPalOptions
{
    public string MerchantId { get; set; } = string.Empty;
}

public class ZarinPalVerificationResult
{
    public bool IsSuccess { get; set; }
    public long? RefId { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IPaymentService
{
    /// <summary>True when this instance is configured to talk to sandbox.zarinpal.com.</summary>
    bool IsSandbox { get; }

    Task<string?> CreateRequestAsync(int amount, string callbackUrl, string description);
    Task<ZarinPalVerificationResult> VerifyRequestAsync(string authority, int amount);
}