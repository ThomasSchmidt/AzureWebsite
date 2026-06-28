using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Models;
using AzureWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace AzureWebsite.Pages.Blog;

/// <summary>
/// Handles individual blog post rendering at /blog/{slug}.
/// Fetches and renders a single blog post by its URL slug.
/// </summary>
public class BlogModel : PageModel
{
    private readonly IBlogService _blogService;

    public BlogModel(IBlogService blogService)
    {
        _blogService = blogService;
    }

    /// <summary>
    /// The blog post being rendered.
    /// </summary>
    public BlogPost? Post { get; set; }

    /// <summary>
    /// The previous post in chronological order (for navigation).
    /// </summary>
    public BlogPost? PreviousPost { get; set; }

    /// <summary>
    /// The next post in chronological order (for navigation).
    /// </summary>
    public BlogPost? NextPost { get; set; }

    /// <summary>
    /// Handles GET requests to /blog/{slug}.
    /// </summary>
    /// <param name="slug">The URL slug of the blog post (e.g., "2026-06-13-getting-started").</param>
    /// <returns>The page if found, otherwise a 404 response.</returns>
    [OutputCache(Duration = 3600)] // Cache for 1 hour
    public async Task<IActionResult> OnGetAsync(string slug)
    {
        // Fetch all posts once to avoid duplicate calls
        var allPosts = await _blogService.GetAllPostsAsync();
        var postsList = allPosts.ToList();
        Post = postsList.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (Post == null)
        {
            return NotFound();
        }

        // Find adjacent posts for navigation
        var currentIndex = postsList.FindIndex(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (currentIndex > 0)
        {
            PreviousPost = postsList[currentIndex - 1];
        }

        if (currentIndex < postsList.Count - 1)
        {
            NextPost = postsList[currentIndex + 1];
        }

        return Page();
    }
}
