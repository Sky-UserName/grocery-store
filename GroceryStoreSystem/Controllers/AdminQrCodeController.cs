using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryStoreSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/qrcode")]
public sealed class AdminQrCodeController(QrCodeService qrCodeService) : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] QrCodeRequest request)
    {
        if (!IsValidUrl(request.Url))
        {
            return BadRequest(new { Message = "请输入有效链接" });
        }

        return File(qrCodeService.CreatePng(request.Url!), "image/png");
    }

    [HttpGet("image")]
    public IActionResult Image([FromQuery] string url)
    {
        if (!IsValidUrl(url))
        {
            return BadRequest("请输入有效链接");
        }

        return File(qrCodeService.CreatePng(url), "image/png");
    }

    [HttpGet("download")]
    public IActionResult Download([FromQuery] string url)
    {
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

    public sealed class QrCodeRequest
    {
        public string? Url { get; set; }
    }
}
