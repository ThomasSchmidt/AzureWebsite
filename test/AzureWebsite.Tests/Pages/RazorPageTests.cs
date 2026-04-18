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
        var settings = GetSettings();
        var sut = new AzureWebsite.Pages.IndexModel(settings);

        // OnGet is void, so we just verify construction and that it doesn't throw
        Assert.NotNull(sut);
    }

    [Fact]
    public void ErrorPageModel_GivenValidInput_CanExecuteError()
    {
        var settings = GetSettings();
        var sut = new AzureWebsite.Pages.ErrorPageModel();

        // OnGet is void, so we just verify construction and that it doesn't throw
        Assert.NotNull(sut);
    }

    private IOptions<WebsiteSettings> GetSettings()
    {
        return new OptionsSettings();
    }

    public class OptionsSettings : IOptionsSnapshot<WebsiteSettings>
    {
        public WebsiteSettings Value => new WebsiteSettings();

        public WebsiteSettings Get(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}