using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroceryStoreSystem.Controllers;

[ApiController]
[Route("api/admin/qrcode")]
public sealed class AdminQrCodeController(QrCodeService qrCodeService) : ControllerBase
{
    [HttpGet("image")]
    public IActionResult Image([FromQuery] string url)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }
        if (!IsValidUrl(url))
        {
            return BadRequest("请输入有效链接");
        }

        return File(qrCodeService.CreatePng(url), "image/png");
    }

    [HttpGet("download")]
    public IActionResult Download([FromQuery] string url)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }
        if (!IsValidUrl(url))
        {
            return BadRequest("请输入有效链接");
        }

        return File(qrCodeService.CreatePng(url), "image/png", "share-qrcode.png");
    }

    private static bool IsValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
    }
}
