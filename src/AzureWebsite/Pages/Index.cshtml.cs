using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace AzureWebsite.Pages;

public class IndexModel : PageModel
{
    private readonly IOptions<WebsiteSettings> _settings;

    public IndexModel(IOptions<WebsiteSettings> settings)
    {
        _settings = settings;
    }

    [OutputCache(Duration = 6000)]
    public void OnGet()
    {
    }
}
