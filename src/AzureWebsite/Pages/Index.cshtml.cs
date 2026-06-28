using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace AzureWebsite.Pages;

public class IndexModel : PageModel
{
    public IndexModel()
    {
    }

    [OutputCache(Duration = 300)]
    public void OnGet()
    {
    }
}
