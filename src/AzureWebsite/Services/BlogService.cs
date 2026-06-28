using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AzureWebsite.Models;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureWebsite.Services;

/// <summary>
/// Configuration settings for the blog feature.
/// </summary>
public class BlogSettings
{
    /// <summary>
    /// Directory containing markdown blog posts (relative to content root).
    /// </summary>
    public string PostsDirectory { get; set; } = "Data/blog";

    /// <summary>
    /// Number of posts to display per page on the listing page.
    /// </summary>
    public int PostsPerPage { get; set; } = 10;

    /// <summary>
    /// Whether to enable category filtering.
    /// </summary>
    public bool EnableCategories { get; set; } = true;

    /// <summary>
    /// Whether to enable tag cloud.
    /// </summary>
    public bool EnableTags { get; set; } = true;

    /// <summary>
    /// Whether to enable comments on posts.
    /// </summary>
    public bool EnableComments { get; set; } = false;
}

/// <summary>
/// Frontmatter metadata extracted from blog post files using manual parsing.
/// </summary>
internal class PostFrontmatter
{
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public string? Summary { get; set; }
    public List<string>? Tags { get; set; }
    public bool? Draft { get; set; }
    public string MarkdownBody { get; set; } = string.Empty;
}

/// <summary>
/// Service for discovering, parsing, and caching blog posts from markdown files.
/// Uses a file system watcher to invalidate cache when posts change.
/// </summary>
public class BlogService : IBlogService
{
    private readonly string _postsDirectory;
    private readonly ILogger<BlogService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _cacheKeyPosts = "blog_all_posts";
    private readonly string _cacheKeyCategories = "blog_categories";
    private readonly string _cacheKeyTags = "blog_tags";
    private readonly MarkdownPipeline _pipeline;

    public BlogService(IOptions<BlogSettings> settings, ILogger<BlogService> logger, IMemoryCache cache)
    {
        _postsDirectory = settings.Value.PostsDirectory;
        _logger = logger;
        _cache = cache;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        var posts = await GetAllPostsAsync();
        return posts.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<BlogPost>> GetAllPostsAsync()
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<BlogPost>>(_cacheKeyPosts, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var posts = new List<BlogPost>();
            var directory = _postsDirectory;

            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Blog posts directory not found: {Directory}", directory);
                return posts;
            }

            var files = Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly).ToList();

            foreach (var file in files)
            {
                try
                {
                    var post = await ParsePostFileAsync(file);
                    if (post != null && !post.IsDraft)
                    {
                        posts.Add(post);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse blog post: {File}", file);
                }
            }

            // Sort by publication date descending
            posts.Sort((a, b) => b.PublishedAt.CompareTo(a.PublishedAt));

            return posts.ToList()!;
        });
        return result!;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<string>>(_cacheKeyCategories, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var posts = await GetAllPostsAsync();
            return posts
                .Where(p => !string.IsNullOrWhiteSpace(p.Category))
                .Select(p => p.Category!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList()!;
        });
        return result!;
    }

    public async Task<IEnumerable<string>> GetTagsAsync()
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<string>>(_cacheKeyTags, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var posts = await GetAllPostsAsync();
            return posts
                .SelectMany(p => p.Tags ?? [])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList()!;
        });
        return result!;
    }

    private async Task<BlogPost?> ParsePostFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Extract frontmatter
        var frontmatter = ExtractFrontmatter(content, fileName);
        if (frontmatter == null)
        {
            return null;
        }

        // Convert markdown to HTML and sanitize to prevent XSS
        var rawHtml = Markdown.ToHtml(frontmatter.MarkdownBody, _pipeline);
        var html = SanitizeHtml(rawHtml);

        var slug = fileName; // Filename serves as the slug

        return new BlogPost
        {
            Slug = slug,
            Title = frontmatter.Title ?? fileName,
            PublishedAt = frontmatter.Date ?? DateTime.UtcNow,
            Author = frontmatter.Author ?? "Admin",
            Category = frontmatter.Category ?? string.Empty,
            Summary = frontmatter.Summary,
            ContentHtml = html,
            Markdown = frontmatter.MarkdownBody,
            FilePath = filePath,
            Tags = frontmatter.Tags ?? new List<string>(),
            IsDraft = frontmatter.Draft == true
        };
    }

    private PostFrontmatter? ExtractFrontmatter(string content, string fileName)
    {
        var frontmatter = new PostFrontmatter();

        if (content.StartsWith("---", StringComparison.Ordinal))
        {
            var parts = content.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var yaml = parts[0].Trim();
                var body = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                // Parse YAML frontmatter manually
                frontmatter = ParseFrontmatter(yaml);
                frontmatter.MarkdownBody = body;
            }
        }
        else
        {
            // No frontmatter — treat entire content as body
            frontmatter.Title = fileName;
            frontmatter.MarkdownBody = content;
        }

        return frontmatter;
    }

    private PostFrontmatter ParseFrontmatter(string yaml)
    {
        var frontmatter = new PostFrontmatter();

        foreach (var line in yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex == -1)
                continue;

            var key = trimmed[..colonIndex].Trim().ToLowerInvariant();
            var value = trimmed[(colonIndex + 1)..].Trim();

            // Remove surrounding quotes
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                value = value[1..^1];
            }

            switch (key)
            {
                case "title":
                    frontmatter.Title = value;
                    break;
                case "date":
                    if (DateTime.TryParse(value, out var date))
                        frontmatter.Date = date;
                    break;
                case "author":
                    frontmatter.Author = value;
                    break;
                case "category":
                    frontmatter.Category = value;
                    break;
                case "summary":
                    frontmatter.Summary = value;
                    break;
                case "tags":
                    // Parse tags: [tag1, tag2, tag3] or tag1, tag2, tag3
                    if (value.StartsWith('[') && value.EndsWith(']'))
                    {
                        value = value.Trim('[', ']');
                    }
                    frontmatter.Tags = value
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
                    break;
                case "draft":
                    frontmatter.Draft = bool.TryParse(value, out var draft) && draft;
                    break;
            }
        }

        return frontmatter;
    }

    /// <summary>
    /// Sanitizes HTML content to remove potentially dangerous attributes like onclick, onerror, etc.
    /// This provides basic XSS protection without requiring additional dependencies.
    /// </summary>
    private static string SanitizeHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        // Remove all event handler attributes (onclick, onerror, onload, onmouseover, etc.)
        var sanitized = Regex.Replace(html, @"\s*on\w+\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\s*on\w+\s*=\s*[^\s>]*", "", RegexOptions.IgnoreCase);

        // Remove javascript: protocol links
        sanitized = Regex.Replace(sanitized, @"href\s*=\s*[""']javascript:[^""']*[""']", "href=\"#\"", RegexOptions.IgnoreCase);

        return sanitized;
    }
}
