using System;
using System.Diagnostics;
using AzureWebsite.Models;
using AzureWebsite.Pages;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Xunit;

namespace AzureWebsite.Tests.Pages;

[Trait("Category", "unittest")]
public class ErrorPageModelTests
{
    private ErrorPageModel CreateModelWithHttpContext(DefaultHttpContext httpContext, IExceptionHandlerPathFeature? exceptionFeature = null)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var pageContext = new PageContext(actionContext);
        var model = new ErrorPageModel
        {
            PageContext = pageContext
        };
        if (exceptionFeature != null)
        {
            httpContext.Features.Set<IExceptionHandlerPathFeature>(exceptionFeature);
        }
        return model;
    }

    [Fact]
    public void ErrorPageModel_GivenDefaultValues_CreatesWithNullErrorInfo()
    {
        var model = new ErrorPageModel();

        Assert.Null(model.ErrorInfo);
    }

    [Fact]
    public void ErrorPageModel_OnGet_GivenNoException_CreatesModelErrorInfo()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";

        var model = CreateModelWithHttpContext(httpContext);

        model.OnGet();

        // When no exception feature is set, ErrorInfo should remain null
        Assert.Null(model.ErrorInfo);
    }

    [Fact]
    public void ErrorPageModel_OnGet_GivenException_CreatesErrorViewModelWithRequestId()
    {
        var exception = new InvalidOperationException("Test exception");
        var feature = Substitute.For<IExceptionHandlerPathFeature>();
        feature.Error.Returns(exception);
        feature.Path.Returns("/error");

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-456";

        var model = CreateModelWithHttpContext(httpContext, feature);

        model.OnGet();

        Assert.NotNull(model.ErrorInfo);
        Assert.NotNull(model.ErrorInfo.RequestId);
    }

    [Fact]
    public void ErrorPageModel_OnGet_GivenExceptionAndActivity_CreatesErrorViewModelWithActivityId()
    {
        var exception = new InvalidOperationException("Test exception");
        var feature = Substitute.For<IExceptionHandlerPathFeature>();
        feature.Error.Returns(exception);
        feature.Path.Returns("/error");

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-789";

        // Set up an Activity to provide an ID
        var activity = new Activity("test-activity");
        activity.Start();
        Activity.Current = activity;

        var model = CreateModelWithHttpContext(httpContext, feature);

        model.OnGet();

        Assert.NotNull(model.ErrorInfo);
        Assert.Equal(activity.Id, model.ErrorInfo.RequestId);

        activity.Stop();
    }

    [Fact]
    public void ErrorPageModel_OnGet_GivenExceptionAndNullActivity_UsesTraceIdentifier()
    {
        var exception = new InvalidOperationException("Test exception");
        var feature = Substitute.For<IExceptionHandlerPathFeature>();
        feature.Error.Returns(exception);
        feature.Path.Returns("/error");

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-identifier-123";

        // Ensure Activity.Current is null
        Activity.Current = null;

        var model = CreateModelWithHttpContext(httpContext, feature);

        model.OnGet();

        Assert.NotNull(model.ErrorInfo);
        Assert.Equal("trace-identifier-123", model.ErrorInfo.RequestId);
    }

    [Fact]
    public void ErrorPageModel_ErrorInfoProperty_CanSetAndGet()
    {
        var model = new ErrorPageModel();
        var errorViewModel = new ErrorViewModel { RequestId = "test-request-id" };

        model.ErrorInfo = errorViewModel;

        Assert.Same(errorViewModel, model.ErrorInfo);
    }
}
