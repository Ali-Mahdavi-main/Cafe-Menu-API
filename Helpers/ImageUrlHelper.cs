using Microsoft.AspNetCore.Http;

namespace CafeMenu.Api.Helpers;

public static class ImageUrlHelper
{
    public static string ToAbsolute(string? relativeOrAbsolute, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            return string.Empty;

        // Already absolute (http/https)
        if (relativeOrAbsolute.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeOrAbsolute.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return relativeOrAbsolute;
        }

        // Relative: prepend scheme + host
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return relativeOrAbsolute.StartsWith('/')
            ? $"{baseUrl}{relativeOrAbsolute}"
            : $"{baseUrl}/{relativeOrAbsolute}";
    }
}