using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GroceryStoreSystem.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminApiController(SqlDataStore store, PasswordHasher hasher) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var admin = await store.GetAdminByUsernameAsync(request.Username.Trim());
        if (admin is null || admin.Status != 1 || !hasher.Verify(request.Password, admin.PasswordHash, admin.PasswordSalt))
        {
            return Unauthorized(new { Message = "账号或密码错误" });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Username)
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        await store.TouchAdminLoginAsync(admin.Id);
        return Ok(new { admin.Username });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpGet("session")]
    public IActionResult Session()
    {
        return Ok(new { Authenticated = User.Identity?.IsAuthenticated == true, Username = User.Identity?.Name });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (!IsSignedIn()) return Unauthorized();
        var stats = await store.GetDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        if (!IsSignedIn()) return Unauthorized();
        var config = await store.GetSiteConfigAsync();
        return Ok(new
        {
            config.SiteName,
            config.SiteSubtitle,
            config.SiteDescription,
            config.AccessPasswordEnabled
        });
    }

    [HttpPut("config")]
    public async Task<IActionResult> SaveConfig([FromBody] ConfigRequest request)
    {
        if (!IsSignedIn()) return Unauthorized();
        var config = await store.GetSiteConfigAsync();
        config.SiteName = request.SiteName.Trim();
        config.SiteSubtitle = request.SiteSubtitle.Trim();
        config.SiteDescription = request.SiteDescription.Trim();
        config.AccessPasswordEnabled = request.AccessPasswordEnabled;
        await store.SaveSiteConfigAsync(config, request.AccessPassword, hasher);
        return Ok();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        if (!IsSignedIn()) return Unauthorized();
        return Ok(await store.GetCategoriesAsync());
    }

    [HttpPost("categories")]
    public async Task<IActionResult> SaveCategory([FromBody] CategoryRequest request)
    {
        if (!IsSignedIn()) return Unauthorized();
        var category = new Category
        {
            Id = request.Id,
            Name = request.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? $"category-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" : request.Slug.Trim(),
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled
        };
        await store.SaveCategoryAsync(category);
        return Ok();
    }

    [HttpDelete("categories/{id:long}")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        if (!IsSignedIn()) return Unauthorized();
        var count = await store.CountCardsByCategoryAsync(id);
        if (count > 0)
        {
            return BadRequest(new { Message = "该分类下已有卡片，不能删除" });
        }
        await store.DeleteCategoryAsync(id);
        return Ok();
    }

    [HttpGet("cards")]
    public async Task<IActionResult> Cards([FromQuery] long? categoryId, [FromQuery] int? status, [FromQuery] string? keyword)
    {
        if (!IsSignedIn()) return Unauthorized();
        var cards = await store.GetCardsAsync(categoryId, status, keyword);
        return Ok(cards.Select(card => new
        {
            card.Id,
            card.CategoryId,
            card.CategoryName,
            card.Title,
            card.Summary,
            card.CoverImageUrl,
            card.Status,
            StatusText = CardStatus.Label(card.Status),
            card.SortOrder,
            Tags = SplitTags(card.Tags),
            card.UpdatedAt
        }));
    }

    [HttpGet("cards/{id:long}")]
    public async Task<IActionResult> Card(long id)
    {
        if (!IsSignedIn()) return Unauthorized();
        var card = await store.GetCardAsync(id);
        if (card is null) return NotFound();
        return Ok(new
        {
            card.Id,
            card.CategoryId,
            card.CategoryName,
            card.Title,
            card.Summary,
            card.CoverImageUrl,
            card.ContentHtml,
            card.Status,
            card.SortOrder,
            Tags = card.Tags.Select(tag => tag.Name)
        });
    }

    [HttpPost("cards")]
    public async Task<IActionResult> SaveCard([FromBody] CardRequest request)
    {
        if (!IsSignedIn()) return Unauthorized();
        var card = new CardDetail
        {
            Id = request.Id,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            CoverImageUrl = request.CoverImageUrl,
            ContentHtml = request.ContentHtml,
            Status = request.Status,
            SortOrder = request.SortOrder,
            Tags = request.Tags.Select(tag => new Tag { Name = tag.Trim() }).Where(tag => tag.Name.Length > 0).ToList()
        };
        var id = await store.SaveCardAsync(card);
        return Ok(new { Id = id });
    }

    [HttpDelete("cards/{id:long}")]
    public async Task<IActionResult> DeleteCard(long id)
    {
        if (!IsSignedIn()) return Unauthorized();
        await store.DeleteCardAsync(id);
        return Ok();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!IsSignedIn()) return Unauthorized();
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { Message = "两次输入的新密码不一致" });
        }
        var admin = await store.GetAdminByUsernameAsync(User.Identity?.Name ?? "");
        if (admin is null || !hasher.Verify(request.CurrentPassword, admin.PasswordHash, admin.PasswordSalt))
        {
            return BadRequest(new { Message = "原密码错误" });
        }
        var (hash, salt) = hasher.Create(request.NewPassword);
        await store.ChangeAdminPasswordAsync(admin.Id, hash, salt);
        return Ok();
    }

    private bool IsSignedIn() => User.Identity?.IsAuthenticated == true;

    private static string[] SplitTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public sealed class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class ConfigRequest
    {
        public string SiteName { get; set; } = "";
        public string SiteSubtitle { get; set; } = "";
        public string SiteDescription { get; set; } = "";
        public string? AccessPassword { get; set; }
        public bool AccessPasswordEnabled { get; set; }
    }

    public sealed class CategoryRequest
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; }
    }

    public sealed class CardRequest
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string? CoverImageUrl { get; set; }
        public string ContentHtml { get; set; } = "";
        public int Status { get; set; }
        public int SortOrder { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
