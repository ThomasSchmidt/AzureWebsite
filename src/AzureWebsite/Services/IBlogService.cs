using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureWebsite.Models;

namespace AzureWebsite.Services;

/// <summary>
/// Service interface for blog post management.
/// Handles discovery, parsing, and caching of markdown blog posts.
/// </summary>
public interface IBlogService
{
    /// <summary>
    /// Retrieves a single blog post by its URL slug.
    /// </summary>
    /// <param name="slug">The URL-friendly slug (e.g., "2026-06-13-getting-started").</param>
    /// <returns>The blog post if found, otherwise null.</returns>
    Task<BlogPost?> GetPostBySlugAsync(string slug);

    /// <summary>
    /// Retrieves all published blog posts, sorted by publication date descending.
    /// </summary>
    Task<IEnumerable<BlogPost>> GetAllPostsAsync();

    /// <summary>
    /// Retrieves all unique categories used across blog posts.
    /// </summary>
    Task<IEnumerable<string>> GetCategoriesAsync();

    /// <summary>
    /// Retrieves all unique tags used across blog posts.
    /// </summary>
    Task<IEnumerable<string>> GetTagsAsync();
}
