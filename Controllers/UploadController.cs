using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("فایلی ارسال نشده است");
        if (file.Length > MaxFileSize)
            return BadRequest("حجم فایل نباید بیشتر از ۵ مگابایت باشد");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("فقط فایل‌های JPG، PNG و WebP مجاز هستند");

        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using var stream = file.OpenReadStream();
        using var original = SKBitmap.Decode(stream);

        // Determine new size (max 1200px)
        const int maxDimension = 1200;
        int newWidth = original.Width;
        int newHeight = original.Height;
        if (newWidth > maxDimension || newHeight > maxDimension)
        {
            var ratio = Math.Min(maxDimension / (double)newWidth, maxDimension / (double)newHeight);
            newWidth = (int)(newWidth * ratio);
            newHeight = (int)(newHeight * ratio);
        }

        // Resize
        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
        if (resized == null)
            return StatusCode(500, "خطا در پردازش تصویر");

        // Encode to JPEG with quality 85
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

        await System.IO.File.WriteAllBytesAsync(filePath, data.ToArray());

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var imageUrl = $"{baseUrl}/uploads/{fileName}";
        return Ok(new { imageUrl });
    }
}