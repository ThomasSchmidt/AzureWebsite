using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Models;
using AzureWebsite.Pages.Blog;
using AzureWebsite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using Xunit;

namespace AzureWebsite.Tests.Pages;

[Trait("Category", "unittest")]
public class BlogPageModelTests
{
    #region BlogListingModel Tests

    [Fact]
    public void BlogListingModel_GivenNullCategory_ReturnsAllPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", PublishedAt = new DateTime(2026, 6, 13) },
            new BlogPost { Slug = "post-2", Title = "Post 2", PublishedAt = new DateTime(2026, 6, 12) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "Category A", "Category B" }));

        var model = new BlogListingModel(mockBlogService);

        Assert.NotNull(model);
        Assert.NotNull(model.Posts);
        Assert.NotNull(model.Categories);
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenNullCategory_ReturnsAllPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", PublishedAt = new DateTime(2026, 6, 13) },
            new BlogPost { Slug = "post-2", Title = "Post 2", PublishedAt = new DateTime(2026, 6, 12) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "Category A", "Category B" }));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet();

        Assert.Equal(2, model.Posts.Count());
        Assert.Null(model.SelectedCategory);
        Assert.Equal(2, model.Categories.Count());
        Assert.Equal(2, model.TotalPosts);
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenCategoryFilter_ReturnsFilteredPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", Category = "Azure", PublishedAt = new DateTime(2026, 6, 13) },
            new BlogPost { Slug = "post-2", Title = "Post 2", Category = "DotNet", PublishedAt = new DateTime(2026, 6, 12) },
            new BlogPost { Slug = "post-3", Title = "Post 3", Category = "Azure", PublishedAt = new DateTime(2026, 6, 11) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "Azure", "DotNet" }));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet(category: "Azure");

        Assert.Equal(2, model.Posts.Count());
        Assert.Equal("Azure", model.SelectedCategory);
        foreach (var post in model.Posts)
        {
            Assert.Equal("Azure", post.Category);
        }
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenNonExistentCategory_ReturnsEmptyPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", Category = "Azure", PublishedAt = new DateTime(2026, 6, 13) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "Azure" }));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet(category: "NonExistent");

        Assert.Empty(model.Posts);
        Assert.Equal("NonExistent", model.SelectedCategory);
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenEmptyPosts_ReturnsEmptyList()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(Enumerable.Empty<BlogPost>()));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult(Enumerable.Empty<string>()));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet();

        Assert.Empty(model.Posts);
        Assert.Empty(model.Categories);
        Assert.Equal(0, model.TotalPosts);
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenCategoryWithSpaces_ReturnsFilteredPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", Category = "Azure Cloud Services", PublishedAt = new DateTime(2026, 6, 13) },
            new BlogPost { Slug = "post-2", Title = "Post 2", Category = "DotNet", PublishedAt = new DateTime(2026, 6, 12) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "Azure Cloud Services", "DotNet" }));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet(category: "Azure Cloud Services");

        Assert.Single(model.Posts);
        Assert.Equal("Azure Cloud Services", model.SelectedCategory);
        Assert.Equal("Azure Cloud Services", model.Posts.First().Category);
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenCaseInsensitiveCategory_ReturnsFilteredPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", Category = "azure", PublishedAt = new DateTime(2026, 6, 13) },
            new BlogPost { Slug = "post-2", Title = "Post 2", Category = "DotNet", PublishedAt = new DateTime(2026, 6, 12) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "azure", "DotNet" }));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet(category: "AZURE");

        Assert.Single(model.Posts);
        Assert.Equal("AZURE", model.SelectedCategory);
    }

    [Fact]
    public async Task BlogListingModel_OnGet_GivenWhitespaceCategory_ReturnsAllPosts()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", Category = "Azure", PublishedAt = new DateTime(2026, 6, 13) }
        }.AsEnumerable();

        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));
        mockBlogService.GetCategoriesAsync().Returns(Task.FromResult<IEnumerable<string>>(new[] { "Azure" }));

        var model = new BlogListingModel(mockBlogService);
        await model.OnGet(category: "   ");

        Assert.Single(model.Posts);
        Assert.Null(model.SelectedCategory);
    }

    [Fact]
    public void BlogListingModel_GivenEmptyPosts_TotalPostsReturnsZero()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var model = new BlogListingModel(mockBlogService);
        model.Posts = Enumerable.Empty<BlogPost>();

        Assert.Equal(0, model.TotalPosts);
    }

    [Fact]
    public void BlogListingModel_GivenMultiplePosts_TotalPostsReturnsCorrectCount()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var model = new BlogListingModel(mockBlogService);
        model.Posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1" },
            new BlogPost { Slug = "post-2", Title = "Post 2" },
            new BlogPost { Slug = "post-3", Title = "Post 3" }
        }.AsEnumerable();

        Assert.Equal(3, model.TotalPosts);
    }

    #endregion

    #region BlogModel Tests

    [Fact]
    public void BlogModel_GivenValidService_CreatesSuccessfully()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var model = new BlogModel(mockBlogService);

        Assert.NotNull(model);
        Assert.Null(model.Post);
        Assert.Null(model.PreviousPost);
        Assert.Null(model.NextPost);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenExistingSlug_ReturnsPageResult()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost { Slug = "test-post", Title = "Test Post", PublishedAt = new DateTime(2026, 6, 13) };
        var posts = new List<BlogPost> { post }.AsEnumerable();

        mockBlogService.GetPostBySlugAsync("test-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult(posts));

        var model = new BlogModel(mockBlogService);
        var result = await model.OnGetAsync("test-post");

        Assert.NotNull(result);
        Assert.NotNull(model.Post);
        Assert.Equal("Test Post", model.Post.Title);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenNonExistentSlug_ReturnsNotFound()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        mockBlogService.GetPostBySlugAsync("non-existent").Returns(Task.FromResult<BlogPost?>(null));

        var model = new BlogModel(mockBlogService);
        var result = await model.OnGetAsync("non-existent");

        Assert.IsType<NotFoundResult>(result);
        Assert.Null(model.Post);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenExistingPost_SetsPreviousAndNext()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", PublishedAt = new DateTime(2026, 6, 11) },
            new BlogPost { Slug = "post-2", Title = "Post 2", PublishedAt = new DateTime(2026, 6, 12) },
            new BlogPost { Slug = "post-3", Title = "Post 3", PublishedAt = new DateTime(2026, 6, 13) }
        };

        var post = posts[1]; // Middle post
        mockBlogService.GetPostBySlugAsync("post-2").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(posts));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("post-2");

        Assert.NotNull(model.PreviousPost);
        Assert.Equal("Post 1", model.PreviousPost.Title);
        Assert.NotNull(model.NextPost);
        Assert.Equal("Post 3", model.NextPost.Title);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenMiddlePost_SetsPreviousAndNext()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", PublishedAt = new DateTime(2026, 6, 11) },
            new BlogPost { Slug = "post-2", Title = "Post 2", PublishedAt = new DateTime(2026, 6, 12) },
            new BlogPost { Slug = "post-3", Title = "Post 3", PublishedAt = new DateTime(2026, 6, 13) }
        };

        var post = posts[1]; // Middle post
        mockBlogService.GetPostBySlugAsync("post-2").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(posts));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("post-2");

        Assert.NotNull(model.PreviousPost);
        Assert.Equal("Post 1", model.PreviousPost.Title);
        Assert.NotNull(model.NextPost);
        Assert.Equal("Post 3", model.NextPost.Title);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenFirstPost_SetsOnlyNext()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", PublishedAt = new DateTime(2026, 6, 11) },
            new BlogPost { Slug = "post-2", Title = "Post 2", PublishedAt = new DateTime(2026, 6, 12) }
        };

        var post = posts[0]; // First post
        mockBlogService.GetPostBySlugAsync("post-1").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(posts));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("post-1");

        Assert.Null(model.PreviousPost);
        Assert.NotNull(model.NextPost);
        Assert.Equal("Post 2", model.NextPost.Title);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenLastPost_SetsOnlyPrevious()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "post-1", Title = "Post 1", PublishedAt = new DateTime(2026, 6, 11) },
            new BlogPost { Slug = "post-2", Title = "Post 2", PublishedAt = new DateTime(2026, 6, 12) }
        };

        var post = posts[1]; // Last post
        mockBlogService.GetPostBySlugAsync("post-2").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(posts));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("post-2");

        Assert.NotNull(model.PreviousPost);
        Assert.Equal("Post 1", model.PreviousPost.Title);
        Assert.Null(model.NextPost);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenSinglePost_SetsNoAdjacent()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var posts = new List<BlogPost>
        {
            new BlogPost { Slug = "only-post", Title = "Only Post", PublishedAt = new DateTime(2026, 6, 13) }
        };

        var post = posts[0];
        mockBlogService.GetPostBySlugAsync("only-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(posts));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("only-post");

        Assert.NotNull(model.Post);
        Assert.Equal("Only Post", model.Post.Title);
        Assert.Null(model.PreviousPost);
        Assert.Null(model.NextPost);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenCaseInsensitiveSlug_ReturnsPost()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost { Slug = "test-post", Title = "Test Post", PublishedAt = new DateTime(2026, 6, 13) };

        mockBlogService.GetPostBySlugAsync("TEST-POST").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        var result = await model.OnGetAsync("TEST-POST");

        Assert.NotNull(result);
        Assert.NotNull(model.Post);
        Assert.Equal("Test Post", model.Post.Title);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenEmptySlug_ReturnsNotFound()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        mockBlogService.GetPostBySlugAsync(string.Empty).Returns(Task.FromResult<BlogPost?>(null));

        var model = new BlogModel(mockBlogService);
        var result = await model.OnGetAsync(string.Empty);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenNullSlug_ReturnsNotFound()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        mockBlogService.GetPostBySlugAsync(null!).Returns(Task.FromResult<BlogPost?>(null));

        var model = new BlogModel(mockBlogService);
        var result = await model.OnGetAsync(null!);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenPostWithTags_SetsPostCorrectly()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost
        {
            Slug = "tagged-post",
            Title = "Tagged Post",
            PublishedAt = new DateTime(2026, 6, 13),
            Tags = new List<string> { "azure", "cloud", "dotnet" }
        };

        mockBlogService.GetPostBySlugAsync("tagged-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("tagged-post");

        Assert.NotNull(model.Post);
        Assert.Equal(3, model.Post.Tags.Count);
        Assert.Contains("azure", model.Post.Tags);
        Assert.Contains("cloud", model.Post.Tags);
        Assert.Contains("dotnet", model.Post.Tags);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenPostWithCategory_SetsPostCorrectly()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost
        {
            Slug = "categorized-post",
            Title = "Categorized Post",
            PublishedAt = new DateTime(2026, 6, 13),
            Category = "Azure"
        };

        mockBlogService.GetPostBySlugAsync("categorized-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("categorized-post");

        Assert.NotNull(model.Post);
        Assert.Equal("Azure", model.Post.Category);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenPostWithSummary_SetsPostCorrectly()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost
        {
            Slug = "summarized-post",
            Title = "Summarized Post",
            PublishedAt = new DateTime(2026, 6, 13),
            Summary = "This is a test summary."
        };

        mockBlogService.GetPostBySlugAsync("summarized-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("summarized-post");

        Assert.NotNull(model.Post);
        Assert.Equal("This is a test summary.", model.Post.Summary);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenPostWithAuthor_SetsPostCorrectly()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost
        {
            Slug = "authored-post",
            Title = "Authored Post",
            PublishedAt = new DateTime(2026, 6, 13),
            Author = "John Doe"
        };

        mockBlogService.GetPostBySlugAsync("authored-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("authored-post");

        Assert.NotNull(model.Post);
        Assert.Equal("John Doe", model.Post.Author);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenPostWithReadingTime_CalculatesCorrectly()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost
        {
            Slug = "reading-post",
            Title = "Reading Post",
            PublishedAt = new DateTime(2026, 6, 13),
            ContentHtml = new string('x', 3600) // 3600 chars = 2 minutes
        };

        mockBlogService.GetPostBySlugAsync("reading-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("reading-post");

        Assert.NotNull(model.Post);
        Assert.Equal(2, model.Post.ReadingTimeMinutes);
    }

    [Fact]
    public async Task BlogModel_OnGetAsync_GivenPostWithEmptyContent_ReturnsOneMinuteReadingTime()
    {
        var mockBlogService = Substitute.For<IBlogService>();
        var post = new BlogPost
        {
            Slug = "empty-content-post",
            Title = "Empty Content Post",
            PublishedAt = new DateTime(2026, 6, 13),
            ContentHtml = string.Empty
        };

        mockBlogService.GetPostBySlugAsync("empty-content-post").Returns(Task.FromResult<BlogPost?>(post));
        mockBlogService.GetAllPostsAsync().Returns(Task.FromResult<IEnumerable<BlogPost>>(new[] { post }));

        var model = new BlogModel(mockBlogService);
        await model.OnGetAsync("empty-content-post");

        Assert.NotNull(model.Post);
        Assert.Equal(1, model.Post.ReadingTimeMinutes);
    }

    #endregion
}
