using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroceryStoreSystem.Controllers;

[ApiController]
[Route("api/mobile")]
public sealed class MobileController(SqlDataStore store, PasswordHasher hasher, MobileAccessTokenService tokenService) : ControllerBase
{
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var config = await store.GetSiteConfigAsync();
        return Ok(new
        {
            config.SiteName,
            config.SiteSubtitle,
            config.SiteDescription,
            config.AccessPasswordEnabled,
            PasswordLength = 6
        });
    }

    [HttpPost("access/verify")]
    public async Task<IActionResult> VerifyAccess([FromBody] VerifyAccessRequest request)
    {
        var config = await store.GetSiteConfigAsync();
        if (!config.AccessPasswordEnabled || hasher.Verify(request.Password ?? "", config.AccessPasswordHash, config.AccessPasswordSalt))
        {
            return Ok(new { Ok = true, Token = tokenService.CreateToken() });
        }

        return BadRequest(new { Ok = false, Message = "访问密码错误" });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        if (!await HasMobileAccessAsync())
        {
            return Unauthorized();
        }

        var categories = await store.GetCategoriesAsync(enabledOnly: true);
        return Ok(categories.Select(c => new
        {
            c.Id,
            c.Name,
            c.Slug,
            c.SortOrder
        }));
    }

    [HttpGet("cards")]
    public async Task<IActionResult> GetCards([FromQuery] long? categoryId)
    {
        if (!await HasMobileAccessAsync())
        {
            return Unauthorized();
        }

        var cards = await store.GetCardsAsync(categoryId: categoryId, mobileOnly: true);
        return Ok(cards.Select(c => new
        {
            c.Id,
            c.CategoryId,
            c.CategoryName,
            c.Title,
            c.Summary,
            c.CoverImageUrl,
            Tags = SplitTags(c.Tags)
        }));
    }

    [HttpGet("cards/{id:long}")]
    public async Task<IActionResult> GetCard(long id)
    {
        if (!await HasMobileAccessAsync())
        {
            return Unauthorized();
        }

        var card = await store.GetCardAsync(id, mobileOnly: true);
        if (card is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            card.Id,
            card.CategoryId,
            card.CategoryName,
            card.Title,
            card.Summary,
            card.CoverImageUrl,
            card.ContentHtml,
            Tags = card.Tags.Select(t => t.Name)
        });
    }

    private async Task<bool> HasMobileAccessAsync()
    {
        var config = await store.GetSiteConfigAsync();
        if (!config.AccessPasswordEnabled)
        {
            return true;
        }

        var authHeader = Request.Headers.Authorization.ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..]
            : Request.Headers["X-Mobile-Access"].ToString();

        return tokenService.Validate(token);
    }

    private static string[] SplitTags(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public sealed class VerifyAccessRequest
    {
        public string? Password { get; set; }
    }
}
