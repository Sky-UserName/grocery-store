using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GroceryStoreSystem.Pages.Admin;

public sealed class QrCodeModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string ShareUrl { get; set; } = "";

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(ShareUrl))
        {
            ShareUrl = $"{Request.Scheme}://{Request.Host}/mobile/login.html";
        }
    }
}
