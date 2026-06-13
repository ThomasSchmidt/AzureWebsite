using System;
using System.Collections.Generic;
using AzureWebsite.Models;
using Xunit;

namespace AzureWebsite.Tests.Models;

[Trait("Category", "unittest")]
public class BlogPostTests
{
    [Fact]
    public void BlogPost_GivenDefaultValues_CreatesWithExpectedDefaults()
    {
        var post = new BlogPost();

        Assert.Equal(string.Empty, post.Slug);
        Assert.Equal(string.Empty, post.Title);
        Assert.Equal(default(DateTime), post.PublishedAt);
        Assert.Equal("Admin", post.Author);
        Assert.Equal(string.Empty, post.Category);
        Assert.Null(post.Summary);
        Assert.Equal(string.Empty, post.ContentHtml);
        Assert.Equal(string.Empty, post.Markdown);
        Assert.Equal(string.Empty, post.FilePath);
        Assert.Empty(post.Tags);
        Assert.False(post.IsDraft);
    }

    [Fact]
    public void BlogPost_GivenAllPropertiesSet_ReturnsExpectedValues()
    {
        var post = new BlogPost
        {
            Slug = "test-post",
            Title = "Test Post",
            PublishedAt = new DateTime(2026, 6, 13),
            Author = "Test Author",
            Category = "Test Category",
            Summary = "Test summary",
            ContentHtml = "<p>Test content</p>",
            Markdown = "# Test",
            FilePath = "/path/to/post.md",
            Tags = new List<string> { "tag1", "tag2" },
            IsDraft = true
        };

        Assert.Equal("test-post", post.Slug);
        Assert.Equal("Test Post", post.Title);
        Assert.Equal(new DateTime(2026, 6, 13), post.PublishedAt);
        Assert.Equal("Test Author", post.Author);
        Assert.Equal("Test Category", post.Category);
        Assert.Equal("Test summary", post.Summary);
        Assert.Equal("<p>Test content</p>", post.ContentHtml);
        Assert.Equal("# Test", post.Markdown);
        Assert.Equal("/path/to/post.md", post.FilePath);
        Assert.Equal(2, post.Tags.Count);
        Assert.Contains("tag1", post.Tags);
        Assert.Contains("tag2", post.Tags);
        Assert.True(post.IsDraft);
    }

    [Theory]
    [InlineData(100)] // Short content
    [InlineData(1800)] // Exactly 1800 chars
    [InlineData(3600)] // 3600 chars
    [InlineData(10000)] // Long content
    public void BlogPost_GivenContentLength_CalculatesReadingTimeCorrectly(int contentLength)
    {
        var post = new BlogPost
        {
            ContentHtml = new string('x', contentLength)
        };

        var expectedMinutes = Math.Max(1, contentLength / 1800);
        Assert.Equal(expectedMinutes, post.ReadingTimeMinutes);
    }

    [Fact]
    public void BlogPost_GivenEmptyContent_ReturnsOneMinuteReadingTime()
    {
        var post = new BlogPost { ContentHtml = string.Empty };
        Assert.Equal(1, post.ReadingTimeMinutes);
    }

    [Fact]
    public void BlogPost_GivenNullTags_CanAddTags()
    {
        var post = new BlogPost();
        post.Tags.Add("new-tag");

        Assert.Single(post.Tags);
        Assert.Equal("new-tag", post.Tags[0]);
    }
}
