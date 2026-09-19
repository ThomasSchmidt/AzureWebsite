using AzureWebsite.Models.Domain;
using Xunit;

namespace AzureWebsite.Tests.Models;

[Trait("Category", "unittest")]
public class GlossaryTermTests
{
    [Fact]
    public void GlossaryTerm_GivenDefaultValues_CreatesWithExpectedDefaults()
    {
        var term = new GlossaryTerm();

        Assert.Equal(string.Empty, term.Name);
        Assert.Equal(string.Empty, term.Description);
    }

    [Fact]
    public void GlossaryTerm_GivenPropertiesSet_ReturnsExpectedValues()
    {
        var term = new GlossaryTerm
        {
            Name = "ADR",
            Description = "Architectural Decision Record."
        };

        Assert.Equal("ADR", term.Name);
        Assert.Equal("Architectural Decision Record.", term.Description);
    }
}