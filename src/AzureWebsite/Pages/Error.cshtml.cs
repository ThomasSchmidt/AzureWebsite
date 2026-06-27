using AzureWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureWebsite.Pages;

public class ErrorPageModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ErrorViewModel? ErrorInfo { get; set; }

    public void OnGet()
    {
        // The error info is passed via the exception handling middleware
        // which sets the IActionContext's ModelState to have an error.
        // We can also read from HttpContext.Items or use a query string.
    }
}
