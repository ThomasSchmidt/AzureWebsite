using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AzureWebsite.Tests.Services;

[Trait("Category", "unittest")]
public class GlossaryServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testDirectory;

    public GlossaryServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"glossary-test-{Guid.NewGuid()}");
        _testDirectory = Path.Combine(_tempDirectory, "glossary");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private static GlossaryService CreateService(string termsDirectory, IMemoryCache? cache = null)
    {
        var settings = Options.Create(new GlossarySettings { TermsDirectory = termsDirectory });
        var logger = Substitute.For<ILogger<GlossaryService>>();
        return new GlossaryService(settings, logger, cache ?? new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public void GlossarySettings_GivenDefaultValues_UsesExpectedDefaultDirectory()
    {
        var settings = new GlossarySettings();
        Assert.Equal("Data/glossary", settings.TermsDirectory);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenEmptyDirectory_ReturnsEmptyList()
    {
        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();
        Assert.Empty(terms);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenNonExistentDirectory_ReturnsEmptyList()
    {
        var service = CreateService(Path.Combine(_tempDirectory, "does-not-exist"));
        var terms = await service.GetAllTermsAsync();
        Assert.Empty(terms);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenSingleTermWithFrontmatter_ReturnsParsedTerm()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "adr.md"),
            "---\nterm: ADR\n---\nShort for Architectural Decision Record.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("ADR", term.Name);
        Assert.Equal("Short for Architectural Decision Record.", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenMultipleTerms_ReturnsSortedAlphabeticallyCaseInsensitive()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "zeta.md"),
            "---\nterm: zeta\n---\nLast term.");
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "alpha.md"),
            "---\nterm: Alpha\n---\nFirst term.");
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "beta.md"),
            "---\nterm: beta\n---\nMiddle term.");

        var service = CreateService(_testDirectory);
        var terms = (await service.GetAllTermsAsync()).ToList();

        Assert.Equal(3, terms.Count);
        Assert.Equal("Alpha", terms[0].Name);
        Assert.Equal("beta", terms[1].Name);
        Assert.Equal("zeta", terms[2].Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenFileWithNoFrontmatter_FallsBackToFileNameAsName()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "no-frontmatter.md"),
            "Just plain description text with no frontmatter.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("no-frontmatter", term.Name);
        Assert.Equal("Just plain description text with no frontmatter.", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenEmptyFile_ReturnsNonNullEmptyDescription()
    {
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "empty.md"), string.Empty);

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("empty", term.Name);
        Assert.Equal(string.Empty, term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenUnclosedFrontmatterMarker_TreatsRemainderAsDescription()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "unclosed.md"),
            "---\nterm: Unclosed\nno closing marker here");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("unclosed", term.Name);
        Assert.Contains("term: Unclosed", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenFrontmatterWithoutTermKey_FallsBackToFileName()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "no-term-key.md"),
            "---\nauthor: someone\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("no-term-key", term.Name);
        Assert.Equal("Description body.", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenFrontmatterWithBlankAndMalformedLines_SkipsThemGracefully()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "blank-lines.md"),
            "---\nmalformed-line-without-colon\n   \nterm: Valid Name\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Valid Name", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenUnreadableFile_LogsErrorAndSkipsFile()
    {
        var goodFilePath = Path.Combine(_testDirectory, "good.md");
        var badFilePath = Path.Combine(_testDirectory, "locked.md");
        await File.WriteAllTextAsync(goodFilePath, "---\nterm: Good\n---\nGood description.");
        await File.WriteAllTextAsync(badFilePath, "---\nterm: Locked\n---\nShould not be read.");

        var service = CreateService(_testDirectory);

        using (var lockStream = new FileStream(badFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var terms = (await service.GetAllTermsAsync()).ToList();

            var term = Assert.Single(terms);
            Assert.Equal("Good", term.Name);
        }
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenBodyContainingHorizontalRule_PreservesFullDescription()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "with-rule.md"),
            "---\nterm: WithRule\n---\nFirst paragraph.\n\n---\n\nSecond paragraph.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("WithRule", term.Name);
        Assert.Contains("First paragraph.", term.Description);
        Assert.Contains("Second paragraph.", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenQuotedTermValue_StripsSurroundingQuotes()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "quoted.md"),
            "---\nterm: \"Agentic Engineering\"\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Agentic Engineering", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenSingleQuotedTermValue_StripsSurroundingQuotes()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "single-quoted.md"),
            "---\nterm: 'Single Quoted'\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Single Quoted", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenSingleCharacterQuoteValue_DoesNotThrow()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "single-char.md"),
            "---\nterm: \"\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("\"", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenMismatchedDoubleQuoteValue_KeepsValueUnchanged()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "mismatched-double.md"),
            "---\nterm: \"Mismatched\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("\"Mismatched", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenMismatchedSingleQuoteValue_KeepsValueUnchanged()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "mismatched-single.md"),
            "---\nterm: 'Mismatched\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("'Mismatched", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenCalledTwice_ReturnsCachedResultOnSecondCall()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "adr.md"),
            "---\nterm: ADR\n---\nDescription.");

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(_testDirectory, cache);

        var firstCall = await service.GetAllTermsAsync();

        // Add a new file after the first call — it should NOT appear in the second call
        // if caching is working, since the cached result is reused.
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "new-term.md"),
            "---\nterm: New Term\n---\nNew description.");

        var secondCall = await service.GetAllTermsAsync();

        Assert.Same(firstCall, secondCall);
        Assert.Single(secondCall);
    }
}