namespace CafeMenu.Api.Services;

public interface IPaymentService
{
    Task<string?> CreateRequestAsync(int amount, string callbackUrl, string description);
    Task<PaymentVerificationResult> VerifyRequestAsync(string authority, int amount);
}
