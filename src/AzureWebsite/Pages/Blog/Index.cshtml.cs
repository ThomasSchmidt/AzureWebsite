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
/// Handles the blog listing page at /blog.
/// Displays all published blog posts with optional category filtering.
/// </summary>
public class BlogListingModel : PageModel
{
    private readonly IBlogService _blogService;

    public BlogListingModel(IBlogService blogService)
    {
        _blogService = blogService;
    }

    /// <summary>
    /// List of blog posts to display on this page.
    /// </summary>
    public IEnumerable<BlogPost> Posts { get; set; } = Enumerable.Empty<BlogPost>();

    /// <summary>
    /// All available categories for filtering.
    /// </summary>
    public IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Currently selected category filter (if any).
    /// </summary>
    public string? SelectedCategory { get; set; }

    /// <summary>
    /// Total number of posts (before pagination, if added later).
    /// </summary>
    public int TotalPosts => Posts.Count();

    /// <summary>
    /// Handles GET requests to /blog.
    /// Optionally filters by category query string parameter.
    /// </summary>
    [OutputCache(Duration = 300)] // Cache for 5 minutes
    public async Task OnGet(string? category = null)
    {
        var allPosts = await _blogService.GetAllPostsAsync();
        Categories = await _blogService.GetCategoriesAsync();

        if (!string.IsNullOrWhiteSpace(category))
        {
            Posts = allPosts.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
            SelectedCategory = category;
        }
        else
        {
            Posts = allPosts;
            SelectedCategory = null;
        }
    }
}
