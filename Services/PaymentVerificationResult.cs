namespace CafeMenu.Api.Services;

public sealed record PaymentVerificationResult(bool IsSuccess, long? RefId, string? Message);
