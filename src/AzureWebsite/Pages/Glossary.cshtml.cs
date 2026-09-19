using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Models.Domain;
using AzureWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace AzureWebsite.Pages;

public class GlossaryModel : PageModel
{
    private readonly IGlossaryService _glossaryService;

    public GlossaryModel(IGlossaryService glossaryService)
    {
        _glossaryService = glossaryService;
    }

    public IReadOnlyList<GlossaryTerm> Terms { get; private set; } = [];

    [OutputCache(Duration = 300)]
    public async Task OnGet()
    {
        var terms = await _glossaryService.GetAllTermsAsync();

        // Defensive sort: the service already sorts alphabetically, but the page
        // guarantees stable ordering regardless of the service implementation.
        Terms = [.. terms.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)];
    }
}