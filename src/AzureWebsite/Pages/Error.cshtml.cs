using System.Diagnostics;
using AzureWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureWebsite.Pages;

public class ErrorPageModel : PageModel
{
    public ErrorViewModel? ErrorInfo { get; set; }

    public void OnGet()
    {
        // Read exception details from HttpContext.Items set by IExceptionHandler middleware
        var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        if (exceptionFeature?.Error != null)
        {
            // Generate a request ID for tracking
            ErrorInfo = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
        }
    }
}
