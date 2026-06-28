using System;
using AzureWebsite.Models;
using Xunit;

namespace AzureWebsite.Tests.Models;

[Trait("Category", "unittest")]
public class ErrorViewModelTests
{
    [Fact]
    public void ErrorViewModel_GivenDefaultValues_CreatesWithExpectedDefaults()
    {
        var model = new ErrorViewModel();

        Assert.Null(model.RequestId);
        Assert.False(model.ShowRequestId);
    }

    [Fact]
    public void ErrorViewModel_GivenNullRequestId_ShowRequestIdReturnsFalse()
    {
        var model = new ErrorViewModel { RequestId = null };

        Assert.False(model.ShowRequestId);
    }

    [Fact]
    public void ErrorViewModel_GivenEmptyRequestId_ShowRequestIdReturnsFalse()
    {
        var model = new ErrorViewModel { RequestId = string.Empty };

        Assert.False(model.ShowRequestId);
    }

    [Fact]
    public void ErrorViewModel_GivenWhitespaceRequestId_ShowRequestIdReturnsTrue()
    {
        var model = new ErrorViewModel { RequestId = "   " };

        Assert.True(model.ShowRequestId);
    }

    [Fact]
    public void ErrorViewModel_GivenNonEmptyRequestId_ShowRequestIdReturnsTrue()
    {
        var model = new ErrorViewModel { RequestId = "request-123" };

        Assert.True(model.ShowRequestId);
        Assert.Equal("request-123", model.RequestId);
    }

    [Fact]
    public void ErrorViewModel_GivenGuidRequestId_ShowRequestIdReturnsTrue()
    {
        var guid = Guid.NewGuid().ToString();
        var model = new ErrorViewModel { RequestId = guid };

        Assert.True(model.ShowRequestId);
        Assert.Equal(guid, model.RequestId);
    }
}
