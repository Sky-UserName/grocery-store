using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryStoreSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/uploads")]
public sealed class AdminUploadsController(IWebHostEnvironment environment, IConfiguration configuration, SqlDataStore store) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    [HttpPost("image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(IFormFile? file, [FromForm] string usageType = "content")
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { Message = "请选择图片文件" });
        }

        var maxBytes = long.TryParse(configuration["Upload:MaxImageBytes"], out var configuredBytes)
            ? configuredBytes
            : 5 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            return BadRequest(new { Message = "图片大小不能超过 5MB" });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { Message = "仅支持 jpg、jpeg、png、webp 图片" });
        }

        var uploadRoot = configuration["Upload:Root"] ?? "uploads";
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd");
        var relativeFolder = Path.Combine(uploadRoot, dateFolder);
        var absoluteFolder = Path.Combine(environment.WebRootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        await store.SaveUploadAsync(new UploadFileRecord
        {
            OriginalName = file.FileName,
            FileName = fileName,
            FileUrl = relativeUrl,
            FileSize = file.Length,
            MimeType = file.ContentType,
            UsageType = usageType
        });

        return Ok(new { Url = relativeUrl });
    }
}
