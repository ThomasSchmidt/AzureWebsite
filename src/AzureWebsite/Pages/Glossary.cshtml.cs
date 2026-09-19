using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace AzureWebsite.Pages;

public class GlossaryModel : PageModel
{
    private static readonly GlossaryTerm[] AllTerms =
    [
        new GlossaryTerm(
            "ADR",
            "Short for Architectural Decision Record, which contains information about important architectural decisions."),
        new GlossaryTerm(
            "Agentic Engineering",
            "How developers use agents to develop code with agent harnesses. Note that this is very much not the same as vibecoding, as agentic engineering always requires a human in the loop to review code and steer agents in the right direction."),
    ];

    public IReadOnlyList<GlossaryTerm> Terms { get; } =
        [.. AllTerms.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)];

    [OutputCache(Duration = 300)]
    public void OnGet()
    {
    }
}

public record GlossaryTerm(string Name, string Description);
