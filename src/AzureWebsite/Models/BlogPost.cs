using System;
using System.Collections.Generic;

namespace AzureWebsite.Models;

/// <summary>
/// Represents a blog post with metadata and rendered content.
/// </summary>
public class BlogPost
{
    /// <summary>
    /// URL-friendly slug derived from the filename (e.g., "2026-06-13-getting-started").
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Post title from YAML frontmatter.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Publication date from YAML frontmatter.
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Author name from YAML frontmatter.
    /// </summary>
    public string Author { get; set; } = "Admin";

    /// <summary>
    /// Category from YAML frontmatter.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Short summary from YAML frontmatter.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// HTML-rendered content from markdown.
    /// </summary>
    public string ContentHtml { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content (useful for debugging or API responses).
    /// </summary>
    public string Markdown { get; set; } = string.Empty;

    /// <summary>
    /// Full file path to the markdown source.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Estimated reading time in minutes.
    /// </summary>
    public int ReadingTimeMinutes => Math.Max(1, ContentHtml.Length / 1800);

    /// <summary>
    /// Tags from YAML frontmatter.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether this post is a draft (should not appear in listings).
    /// </summary>
    public bool IsDraft { get; set; }
}
