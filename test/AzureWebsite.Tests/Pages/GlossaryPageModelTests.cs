using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Models.Domain;
using AzureWebsite.Pages;
using AzureWebsite.Services;
using NSubstitute;
using Xunit;

namespace AzureWebsite.Tests.Pages;

[Trait("Category", "unittest")]
public class GlossaryPageModelTests
{
    [Fact]
    public void GlossaryModel_GivenConstruction_InitializesEmptyTerms()
    {
        var mockGlossaryService = Substitute.For<IGlossaryService>();
        var model = new GlossaryModel(mockGlossaryService);

        Assert.NotNull(model.Terms);
        Assert.Empty(model.Terms);
    }

    [Fact]
    public async Task OnGet_GivenTermsFromService_PopulatesTermsSortedAlphabetically()
    {
        var mockGlossaryService = Substitute.For<IGlossaryService>();
        var terms = new List<GlossaryTerm>
        {
            new GlossaryTerm { Name = "zeta", Description = "Last." },
            new GlossaryTerm { Name = "Alpha", Description = "First." },
            new GlossaryTerm { Name = "beta", Description = "Middle." }
        }.AsEnumerable();

        mockGlossaryService.GetAllTermsAsync().Returns(Task.FromResult(terms));

        var model = new GlossaryModel(mockGlossaryService);
        await model.OnGet();

        Assert.Equal(3, model.Terms.Count);
        Assert.Equal("Alpha", model.Terms[0].Name);
        Assert.Equal("beta", model.Terms[1].Name);
        Assert.Equal("zeta", model.Terms[2].Name);
    }

    [Fact]
    public async Task OnGet_GivenEmptyTermsFromService_ReturnsEmptyTerms()
    {
        var mockGlossaryService = Substitute.For<IGlossaryService>();
        mockGlossaryService.GetAllTermsAsync().Returns(Task.FromResult(Enumerable.Empty<GlossaryTerm>()));

        var model = new GlossaryModel(mockGlossaryService);
        await model.OnGet();

        Assert.Empty(model.Terms);
    }
}