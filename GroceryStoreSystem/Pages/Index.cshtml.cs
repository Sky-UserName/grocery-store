using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GroceryStoreSystem.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Index");
    }
}
