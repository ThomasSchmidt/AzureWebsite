using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Models;
using AzureWebsite.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AzureWebsite.Tests.Services;

[Trait("Category", "unittest")]
public class BlogServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testDirectory;

    public BlogServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"blog-test-{Guid.NewGuid()}");
        _testDirectory = Path.Combine(_tempDirectory, "blog");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private BlogService CreateService(string? postsDirectory = null)
    {
        var settings = Options.Create(new BlogSettings { PostsDirectory = postsDirectory ?? _testDirectory });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Substitute.For<ILogger<BlogService>>();
        return new BlogService(settings, logger, cache);
    }

    #region GetAllPostsAsync

    [Fact]
    public async Task GetAllPostsAsync_GivenEmptyDirectory_ReturnsEmptyList()
    {
        var service = CreateService();
        var posts = await service.GetAllPostsAsync();
        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenNonExistentDirectory_ReturnsEmptyList()
    {
        var settings = Options.Create(new BlogSettings { PostsDirectory = @"C:\nonexistent\path" });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Substitute.For<ILogger<BlogService>>();
        var service = new BlogService(settings, logger, cache);
        var posts = await service.GetAllPostsAsync();
        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenSinglePost_ReturnsPostWithAllFields()
    {
        var postContent = "title: Test Post\ndate: 2026-06-13\nauthor: Test Author\ncategory: Test Category\nsummary: Test summary\ntags: [tag1, tag2]\n---\n\n# Hello World\n\nThis is a test post.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-test-post.md"), "---\n" + postContent);
        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        var post = posts.First();
        Assert.Equal("2026-06-13-test-post", post.Slug);
        Assert.Equal("Test Post", post.Title);
        Assert.Equal(new DateTime(2026, 6, 13), post.PublishedAt);
        Assert.Equal("Test Author", post.Author);
        Assert.Equal("Test Category", post.Category);
        Assert.Equal("Test summary", post.Summary);
        Assert.Equal(2, post.Tags.Count);
        Assert.Contains("tag1", post.Tags);
        Assert.Contains("tag2", post.Tags);
        Assert.False(post.IsDraft);
        Assert.Contains("h1", post.ContentHtml);
        Assert.Contains("Hello World", post.ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenMultiplePosts_ReturnsSortedByDateDescending()
    {
        var olderPost = "title: Older Post\ndate: 2026-06-10\n---\nOlder content.";
        var newerPost = "title: Newer Post\ndate: 2026-06-13\n---\nNewer content.";

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-10-older.md"), "---\n" + olderPost);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-newer.md"), "---\n" + newerPost);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();
        var postList = posts.ToList();

        Assert.Equal(2, postList.Count);
        Assert.Equal("Newer Post", postList[0].Title);
        Assert.Equal("Older Post", postList[1].Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenDraftPost_ExcludesFromList()
    {
        var draftPost = "title: Draft Post\ndate: 2026-06-13\ndraft: true\n---\nDraft content.";
        var publishedPost = "title: Published Post\ndate: 2026-06-13\n---\nPublished content.";

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-draft.md"), "---\n" + draftPost);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-published.md"), "---\n" + publishedPost);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("Published Post", posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithoutFrontmatter_ReturnsPostWithDefaults()
    {
        var noFrontmatter = "# No Frontmatter\n\nThis post has no YAML frontmatter.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-no-frontmatter.md"), noFrontmatter);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        var post = posts.First();
        Assert.Equal("2026-06-13-no-frontmatter", post.Slug);
        Assert.Equal("2026-06-13-no-frontmatter", post.Title);
        Assert.Equal("Admin", post.Author);
        Assert.Equal(string.Empty, post.Category);
        Assert.Empty(post.Tags);
        Assert.False(post.IsDraft);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithMarkdownContent_ConvertsToHtml()
    {
        var postContent = "title: Markdown Post\ndate: 2026-06-13\n---\n\n# Heading 1\n\n## Heading 2\n\n**Bold** and *italic*.\n\n- Item 1\n- Item 2\n\n`code`\n\n> Quote\n\n[Link](https://example.com)";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-markdown.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        var post = posts.First();
        Assert.Contains("h1", post.ContentHtml);
        Assert.Contains("h2", post.ContentHtml);
        Assert.Contains("strong", post.ContentHtml);
        Assert.Contains("em>", post.ContentHtml);
        Assert.Contains("ul>", post.ContentHtml);
        Assert.Contains("li>", post.ContentHtml);
        Assert.Contains("code", post.ContentHtml);
        Assert.Contains("blockquote", post.ContentHtml);
        Assert.Contains("href", post.ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithQuotesInFrontmatter_ParsesCorrectly()
    {
        var postContent = "title: \"Quoted Title\"\ndate: 2026-06-13\nauthor: 'Single Quoted'\nsummary: \"Double Quoted\"\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-quoted.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        var post = posts.First();
        Assert.Equal("Quoted Title", post.Title);
        Assert.Equal("Single Quoted", post.Author);
        Assert.Equal("Double Quoted", post.Summary);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithCommaSeparatedTags_ParsesCorrectly()
    {
        var postContent = "title: Comma Tags\ndate: 2026-06-13\ntags: tag1, tag2, tag3\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-comma-tags.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(3, posts.First().Tags.Count);
        Assert.Contains("tag1", posts.First().Tags);
        Assert.Contains("tag2", posts.First().Tags);
        Assert.Contains("tag3", posts.First().Tags);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithWhitespaceInTags_TrimsCorrectly()
    {
        var postContent = "title: Whitespace Tags\ndate: 2026-06-13\ntags: [  tag1  ,  tag2  ,  tag3  ]\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-whitespace-tags.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(3, posts.First().Tags.Count);
        Assert.Equal("tag1", posts.First().Tags[0]);
        Assert.Equal("tag2", posts.First().Tags[1]);
        Assert.Equal("tag3", posts.First().Tags[2]);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithDraftFalse_IncludesInList()
    {
        var postContent = "title: Explicit Non-Draft\ndate: 2026-06-13\ndraft: false\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-explicit-nondraft.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.False(posts.First().IsDraft);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithDraftTrue_ExcludesFromList()
    {
        var postContent = "title: Explicit Draft\ndate: 2026-06-13\ndraft: true\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-explicit-draft.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();
        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithFutureDate_ReturnsPost()
    {
        var postContent = "title: Future Post\ndate: 2030-12-31\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2030-12-31-future.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(new DateTime(2030, 12, 31), posts.First().PublishedAt);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithPastDate_ReturnsPost()
    {
        var postContent = "title: Past Post\ndate: 2020-01-01\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2020-01-01-past.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(new DateTime(2020, 1, 1), posts.First().PublishedAt);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenMultiplePostsInSameDirectory_ReturnsAll()
    {
        for (var i = 0; i < 5; i++)
        {
            var postContent = $"title: Post {i}\ndate: 2026-06-{i + 1:D2}\ncategory: Category {i}\ntags: [tag{i}]\n---\nContent {i}.";
            await File.WriteAllTextAsync(Path.Combine(_testDirectory, $"2026-06-{i + 1:D2}-post{i}.md"), "---\n" + postContent);
        }

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();
        Assert.Equal(5, posts.Count());
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithOnlyDashesInFrontmatter_ReturnsPostWithDefaults()
    {
        var postContent = "---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-only-dashes.md"), postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("2026-06-13-only-dashes", posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithMultipleDashesInContent_ReturnsPost()
    {
        var postContent = "title: Multiple Dashes\ndate: 2026-06-13\n---\nContent with horizontal rule.\n\n---\nMore content.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-multiple-dashes.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("Multiple Dashes", posts.First().Title);
        Assert.Contains("horizontal rule", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithUnknownFrontmatterKeys_IgnoresUnknownKeys()
    {
        var postContent = "title: Unknown Keys Post\ndate: 2026-06-13\nunknownKey: unknownValue\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-unknown-keys.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("Unknown Keys Post", posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithMixedCaseDraftTrue_HandlesCorrectly()
    {
        var postContent = "title: Mixed Case Draft\ndate: 2026-06-13\ndraft: TRUE\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-mixed-case-draft.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();
        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithMixedCaseDraftFalse_HandlesCorrectly()
    {
        var postContent = "title: Mixed Case Draft False\ndate: 2026-06-13\ndraft: False\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-mixed-case-draft-false.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.False(posts.First().IsDraft);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithCategoryContainingSpaces_HandlesCorrectly()
    {
        var postContent = "title: Category With Spaces\ndate: 2026-06-13\ncategory: Azure Cloud Services\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-category-spaces.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("Azure Cloud Services", posts.First().Category);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithAuthorContainingSpaces_HandlesCorrectly()
    {
        var postContent = "title: Author With Spaces\ndate: 2026-06-13\nauthor: John Doe Smith\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-author-spaces.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("John Doe Smith", posts.First().Author);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithEmptySummary_ReturnsEmptyString()
    {
        var postContent = "title: Empty Summary\ndate: 2026-06-13\nsummary:\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-empty-summary.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(string.Empty, posts.First().Summary);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithEmptyStringSummary_ReturnsEmptyString()
    {
        var postContent = "title: Empty String Summary\ndate: 2026-06-13\nsummary: \"\"\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-empty-string-summary.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(string.Empty, posts.First().Summary);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithWindowsLineEndings_HandlesCorrectly()
    {
        var postContent = "---\r\ntitle: Windows Line Endings\r\ndate: 2026-06-13\r\n---\r\n\r\nContent with CRLF.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-windows-line-endings.md"), postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("Windows Line Endings", posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithUnixLineEndings_HandlesCorrectly()
    {
        var postContent = "---\ntitle: Unix Line Endings\ndate: 2026-06-13\n---\n\nContent with LF.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-unix-line-endings.md"), postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("Unix Line Endings", posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithHyphensAndUnderscoresInSlug_HandlesCorrectly()
    {
        var postContent = "title: Hyphens And Underscores\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-hyphens_and_underscores.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("2026-06-13-hyphens_and_underscores", posts.First().Slug);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithNumbersInSlug_HandlesCorrectly()
    {
        var postContent = "title: Numbers In Slug\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-post-123-456.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("2026-06-13-post-123-456", posts.First().Slug);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithMixedCaseFilename_HandlesCorrectly()
    {
        var postContent = "title: Mixed Case Filename\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-MixedCaseFilename.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal("2026-06-13-MixedCaseFilename", posts.First().Slug);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithSpecialCharactersInTags_ParsesCorrectly()
    {
        var postContent = "title: Special Tags\ndate: 2026-06-13\ntags: [dotnet-8, .NET-9, C#-12]\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-special-tags.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(3, posts.First().Tags.Count);
        Assert.Contains("dotnet-8", posts.First().Tags);
        Assert.Contains(".NET-9", posts.First().Tags);
        Assert.Contains("C#-12", posts.First().Tags);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithUnicodeInFrontmatter_HandlesCorrectly()
    {
        var postContent = "title: Unicode Post - \u65e5\u672c\u8a9e\ndate: 2026-06-13\nauthor: \u4e2d\u6587\u4f5c\u8005\ncategory: \u591a\u8a00\u8a9e\n---\nContent with unicode \u4e2d\u6587.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-unicode.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Contains("Unicode Post", posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithLongTitle_HandlesCorrectly()
    {
        var longTitle = new string('x', 500);
        var postContent = $"title: {longTitle}\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-long-title.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(longTitle, posts.First().Title);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithLongTags_HandlesCorrectly()
    {
        var tags = string.Join(", ", Enumerable.Range(1, 50).Select(i => $"tag{i}"));
        var postContent = $"title: Long Tags Post\ndate: 2026-06-13\ntags: [{tags}]\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-long-tags.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Equal(50, posts.First().Tags.Count);
    }

    #endregion

    #region GetPostBySlugAsync

    [Fact]
    public async Task GetPostBySlugAsync_GivenExistingSlug_ReturnsPost()
    {
        var postContent = "title: Findable Post\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-findable.md"), "---\n" + postContent);

        var service = CreateService();
        var post = await service.GetPostBySlugAsync("2026-06-13-findable");

        Assert.NotNull(post);
        Assert.Equal("Findable Post", post.Title);
    }

    [Fact]
    public async Task GetPostBySlugAsync_GivenNonExistentSlug_ReturnsNull()
    {
        var service = CreateService();
        var post = await service.GetPostBySlugAsync("non-existent-slug");
        Assert.Null(post);
    }

    [Fact]
    public async Task GetPostBySlugAsync_GivenCaseInsensitiveSlug_ReturnsPost()
    {
        var postContent = "title: Case Test\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-case-test.md"), "---\n" + postContent);

        var service = CreateService();
        var post = await service.GetPostBySlugAsync("2026-06-13-Case-Test");

        Assert.NotNull(post);
        Assert.Equal("Case Test", post.Title);
    }

    [Fact]
    public async Task GetPostBySlugAsync_GivenEmptySlug_ReturnsNull()
    {
        var service = CreateService();
        var post = await service.GetPostBySlugAsync(string.Empty);
        Assert.Null(post);
    }

    [Fact]
    public async Task GetPostBySlugAsync_GivenNullSlug_ReturnsNull()
    {
        var service = CreateService();
        var post = await service.GetPostBySlugAsync(null!);
        Assert.Null(post);
    }

    [Fact]
    public async Task GetPostBySlugAsync_GivenWhitespaceSlug_ReturnsNull()
    {
        var service = CreateService();
        var post = await service.GetPostBySlugAsync("   ");
        Assert.Null(post);
    }

    #endregion

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesAsync_GivenMultiplePosts_ReturnsUniqueCategories()
    {
        var post1 = "title: Post 1\ndate: 2026-06-13\ncategory: Azure\n---\nContent.";
        var post2 = "title: Post 2\ndate: 2026-06-12\ncategory: DotNet\n---\nContent.";
        var post3 = "title: Post 3\ndate: 2026-06-11\ncategory: Azure\n---\nContent.";

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-post1.md"), "---\n" + post1);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-12-post2.md"), "---\n" + post2);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-11-post3.md"), "---\n" + post3);

        var service = CreateService();
        var categories = await service.GetCategoriesAsync();
        var categoryList = categories.ToList();

        Assert.Equal(2, categoryList.Count);
        Assert.Contains("Azure", categoryList);
        Assert.Contains("DotNet", categoryList);
    }

    [Fact]
    public async Task GetCategoriesAsync_GivenPostsWithoutCategory_ExcludesEmpty()
    {
        var post1 = "title: Post 1\ndate: 2026-06-13\n---\nContent.";
        var post2 = "title: Post 2\ndate: 2026-06-12\ncategory: Test\n---\nContent.";

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-post1.md"), "---\n" + post1);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-12-post2.md"), "---\n" + post2);

        var service = CreateService();
        var categories = await service.GetCategoriesAsync();
        var categoryList = categories.ToList();

        Assert.Single(categoryList);
        Assert.Equal("Test", categoryList[0]);
    }

    [Fact]
    public async Task GetCategoriesAsync_GivenEmptyDirectory_ReturnsEmpty()
    {
        var service = CreateService();
        var categories = await service.GetCategoriesAsync();
        Assert.Empty(categories);
    }

    #endregion

    #region GetTagsAsync

    [Fact]
    public async Task GetTagsAsync_GivenMultiplePosts_ReturnsUniqueTags()
    {
        var post1 = "title: Post 1\ndate: 2026-06-13\ntags: [azure, cloud]\n---\nContent.";
        var post2 = "title: Post 2\ndate: 2026-06-12\ntags: [dotnet, azure]\n---\nContent.";

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-post1.md"), "---\n" + post1);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-12-post2.md"), "---\n" + post2);

        var service = CreateService();
        var tags = await service.GetTagsAsync();
        var tagList = tags.ToList();

        Assert.Equal(3, tagList.Count);
        Assert.Contains("azure", tagList);
        Assert.Contains("cloud", tagList);
        Assert.Contains("dotnet", tagList);
    }

    [Fact]
    public async Task GetTagsAsync_GivenPostWithoutTags_ReturnsEmpty()
    {
        var post1 = "title: Post 1\ndate: 2026-06-13\n---\nContent.";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-post1.md"), "---\n" + post1);

        var service = CreateService();
        var tags = await service.GetTagsAsync();
        Assert.Empty(tags);
    }

    [Fact]
    public async Task GetTagsAsync_GivenEmptyDirectory_ReturnsEmpty()
    {
        var service = CreateService();
        var tags = await service.GetTagsAsync();
        Assert.Empty(tags);
    }

    #endregion

    #region HTML Sanitization (XSS Protection)

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithOnerrorAttribute_RemovesEventHandler()
    {
        var postContent = "title: XSS Test\ndate: 2026-06-13\n---\n\n<img src=x onerror=alert('XSS')>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-xss-onerror.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.DoesNotContain("onerror", posts.First().ContentHtml);
        Assert.DoesNotContain("alert", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithOnclickAttribute_RemovesEventHandler()
    {
        var postContent = "title: Click XSS Test\ndate: 2026-06-13\n---\n\n<button onclick=alert('clicked')>Click me</button>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-xss-onclick.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.DoesNotContain("onclick", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithJavascriptProtocol_RemovesJavascriptLink()
    {
        var postContent = "title: JS Protocol Test\ndate: 2026-06-13\n---\n\n<a href=\"javascript:alert('XSS')\">Click</a>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-xss-javascript.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.DoesNotContain("javascript:", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithOnloadAttribute_RemovesEventHandler()
    {
        var postContent = "title: Load XSS Test\ndate: 2026-06-13\n---\n\n<body onload=alert('XSS')>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-xss-onload.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.DoesNotContain("onload", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithMultipleEventHandlers_RemovesAll()
    {
        var postContent = "title: Multiple XSS Test\ndate: 2026-06-13\n---\n\n<div onmouseover=alert(1) onmouseout=alert(2) onclick=alert(3)>Content</div>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-xss-multiple.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.DoesNotContain("onmouseover", posts.First().ContentHtml);
        Assert.DoesNotContain("onmouseout", posts.First().ContentHtml);
        Assert.DoesNotContain("onclick", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithSafeHtml_PreservesLegitimateContent()
    {
        var postContent = "title: Safe HTML Test\ndate: 2026-06-13\n---\n\n<p>This is <strong>safe</strong> HTML with a <a href=\"https://example.com\">link</a>.</p>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-safe-html.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.Contains("<p>", posts.First().ContentHtml);
        Assert.Contains("<strong>", posts.First().ContentHtml);
        Assert.Contains("href=\"https://example.com\"", posts.First().ContentHtml);
    }

    [Fact]
    public async Task GetAllPostsAsync_GivenPostWithOnfocusAttribute_RemovesEventHandler()
    {
        var postContent = @"title: Focus XSS Test
date: 2026-06-13
---

<a href=""https://example.com"" onfocus=alert('XSS')>Link</a>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "2026-06-13-xss-onfocus.md"), "---\n" + postContent);

        var service = CreateService();
        var posts = await service.GetAllPostsAsync();

        Assert.Single(posts);
        Assert.DoesNotContain("onfocus", posts.First().ContentHtml);
    }

    #endregion
}
