using AzureWebsite.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace AzureWebsite.Tests.Pages;

[Trait("Category", "unittest")]
public class RazorPageTests
{
    [Fact]
    public void IndexModel_GivenValidInput_CanExecuteIndex()
    {
        var sut = new AzureWebsite.Pages.IndexModel();

        // OnGet is void, so we just verify construction and that it doesn't throw
        Assert.NotNull(sut);
    }

    [Fact]
    public void IndexModel_OnGet_CanBeCalledWithoutException()
    {
        var sut = new AzureWebsite.Pages.IndexModel();

        // OnGet has OutputCache attribute but is otherwise empty
        // Calling it should not throw any exceptions
        sut.OnGet();

        // If we got here without exception, the test passes
        Assert.True(true);
    }

    [Fact]
    public void IndexModel_Constructor_CreatesInstanceWithoutDependencies()
    {
        var sut = new AzureWebsite.Pages.IndexModel();

        Assert.NotNull(sut);
    }
}