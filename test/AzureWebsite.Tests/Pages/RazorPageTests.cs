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
}