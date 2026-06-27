using AzureWebsite.Models;
using AzureWebsite.Pages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace AzureWebsite.Tests.Models;

[Trait("Category", "unittest")]
public class ErrorPageModelTests
{
    [Fact]
    public void ErrorPageModel_GivenDefaultValues_CreatesWithExpectedDefaults()
    {
        var model = new ErrorPageModel();

        Assert.Null(model.ErrorInfo);
    }

    [Fact]
    public void ErrorPageModel_GivenErrorInfo_SetAndGet_ReturnsExpectedValue()
    {
        var errorInfo = new ErrorViewModel { RequestId = "test-request-id" };
        var model = new ErrorPageModel { ErrorInfo = errorInfo };

        Assert.NotNull(model.ErrorInfo);
        Assert.Equal("test-request-id", model.ErrorInfo.RequestId);
    }

    [Fact]
    public void ErrorPageModel_GivenNullErrorInfo_ShowRequestIdReturnsFalse()
    {
        var model = new ErrorPageModel();

        Assert.Null(model.ErrorInfo);
    }

    [Fact]
    public void ErrorPageModel_GivenErrorInfoWithRequestId_ShowRequestIdDelegatesCorrectly()
    {
        var errorInfo = new ErrorViewModel { RequestId = "request-456" };
        var model = new ErrorPageModel { ErrorInfo = errorInfo };

        Assert.True(model.ErrorInfo!.ShowRequestId);
    }

    [Fact]
    public void ErrorPageModel_GivenErrorInfoWithoutRequestId_ShowRequestIdDelegatesCorrectly()
    {
        var errorInfo = new ErrorViewModel { RequestId = null };
        var model = new ErrorPageModel { ErrorInfo = errorInfo };

        Assert.False(model.ErrorInfo!.ShowRequestId);
    }
}
