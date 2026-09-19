using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
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

    private static GlossaryService CreateService(string termsDirectory, IMemoryCache? cache = null, string? contentRootPath = null)
    {
        var settings = Options.Create(new GlossarySettings { TermsDirectory = termsDirectory });
        var logger = Substitute.For<ILogger<GlossaryService>>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(contentRootPath ?? string.Empty);
        return new GlossaryService(settings, logger, cache ?? new MemoryCache(new MemoryCacheOptions()), hostEnvironment);
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
    public async Task GetAllTermsAsync_GivenBlankTermValue_FallsBackToFileName()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "blank-term.md"),
            "---\nterm:\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("blank-term", term.Name);
        Assert.NotEqual(string.Empty, term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenEmptyQuotedTermValue_FallsBackToFileName()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "empty-quoted-term.md"),
            "---\nterm: \"\"\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("empty-quoted-term", term.Name);
        Assert.NotEqual(string.Empty, term.Name);
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
    public async Task GetAllTermsAsync_GivenDashesInsideFrontmatterValue_DoesNotMistakeThemForClosingMarker()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "dashes-in-value.md"),
            "---\nterm: \"A---B\"\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("A---B", term.Name);
        Assert.Equal("Description body.", term.Description);
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

    [Fact]
    public async Task GetAllTermsAsync_GivenRelativeTermsDirectoryAndContentRoot_ResolvesAgainstContentRoot()
    {
        var contentRoot = Path.Combine(_tempDirectory, "content-root");
        var relativeTermsDirectory = "relative-terms";
        var absoluteTermsDirectory = Path.Combine(contentRoot, relativeTermsDirectory);
        Directory.CreateDirectory(absoluteTermsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(absoluteTermsDirectory, "adr.md"),
            "---\nterm: ADR\n---\nShort for Architectural Decision Record.");

        var service = CreateService(relativeTermsDirectory, contentRootPath: contentRoot);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("ADR", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenEnumerationFailure_LogsWarningAndReturnsEmptyList()
    {
        var settings = Options.Create(new GlossarySettings { TermsDirectory = _testDirectory });
        var logger = Substitute.For<ILogger<GlossaryService>>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(string.Empty);
        var service = new ThrowingGlossaryService(settings, logger, new MemoryCache(new MemoryCacheOptions()), hostEnvironment);

        var terms = await service.GetAllTermsAsync();

        Assert.Empty(terms);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenTitleKeyWithNoTermKey_UsesTitleAsName()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "title-only.md"),
            "---\ntitle: Titled Term\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Titled Term", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenNameKeyWithNoTermOrTitleKey_UsesNameAsName()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "name-only.md"),
            "---\nname: Named Term\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Named Term", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenTermTitleAndNameAllPresent_TermTakesPriority()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "priority.md"),
            "---\nname: Named Term\ntitle: Titled Term\nterm: Termed Term\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Termed Term", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenDescriptionKeyWithBody_DescriptionKeyWinsOverBody()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "description-with-body.md"),
            "---\nterm: Described\ndescription: From frontmatter\n---\nFrom body, should be ignored.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Described", term.Name);
        Assert.Equal("From frontmatter", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenDescriptionKeyWithNoBody_UsesFrontmatterDescription()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "description-no-body.md"),
            "---\nterm: DescribedNoBody\ndescription: Only frontmatter description\n---\n");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("DescribedNoBody", term.Name);
        Assert.Equal("Only frontmatter description", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenQuotedDescriptionValue_StripsSurroundingQuotes()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "quoted-description.md"),
            "---\nterm: QuotedDescription\ndescription: \"Quoted description value\"\n---\nBody text ignored.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Quoted description value", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenSeedFileAdr_ParsesCorrectly()
    {
        var seedFilePath = Path.Combine(
            AppContext.BaseDirectory, "Data", "glossary", "adr.md");
        Assert.True(File.Exists(seedFilePath), $"Seed file not found at {seedFilePath}");

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "adr.md"),
            await File.ReadAllTextAsync(seedFilePath));

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("ADR", term.Name);
        Assert.Contains("Architectural Decision Record", term.Description);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenSeedFileAgenticEngineering_ParsesCorrectly()
    {
        var seedFilePath = Path.Combine(
            AppContext.BaseDirectory, "Data", "glossary", "agentic-engineering.md");
        Assert.True(File.Exists(seedFilePath), $"Seed file not found at {seedFilePath}");

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "agentic-engineering.md"),
            await File.ReadAllTextAsync(seedFilePath));

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Agentic Engineering", term.Name);
        Assert.Contains("agentic engineering", term.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenBlankTermValueAndPopulatedTitleValue_FallsBackToTitle()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "blank-term-fallback.md"),
            "---\nterm:\ntitle: Real Name\n---\nDescription body.");

        var service = CreateService(_testDirectory);
        var terms = await service.GetAllTermsAsync();

        var term = Assert.Single(terms);
        Assert.Equal("Real Name", term.Name);
    }

    [Fact]
    public async Task GetAllTermsAsync_GivenEnumerationFailureFollowedBySuccess_DoesNotCacheFailureResult()
    {
        var settings = Options.Create(new GlossarySettings { TermsDirectory = _testDirectory });
        var logger = Substitute.For<ILogger<GlossaryService>>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(string.Empty);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var throwingService = new ThrowingGlossaryService(settings, logger, cache, hostEnvironment);

        var firstResult = await throwingService.GetAllTermsAsync();
        Assert.Empty(firstResult);

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "adr.md"),
            "---\nterm: ADR\n---\nShort for Architectural Decision Record.");

        var normalService = CreateService(_testDirectory, cache);
        var secondResult = await normalService.GetAllTermsAsync();

        var term = Assert.Single(secondResult);
        Assert.Equal("ADR", term.Name);
    }

    /// <summary>
    /// Test-only subclass that forces the directory-enumeration seam to throw, simulating a
    /// TOCTOU race or permission failure between the <c>Directory.Exists</c> check and file
    /// enumeration. A real, deterministic, cross-platform OS-level repro (e.g. deleting the
    /// directory mid-race, or revoking ACLs) proved unreliable/non-portable, so this seam is
    /// used to exercise the catch branch directly instead.
    /// </summary>
    private sealed class ThrowingGlossaryService(
        IOptions<GlossarySettings> settings,
        ILogger<GlossaryService> logger,
        IMemoryCache cache,
        IHostEnvironment hostEnvironment) : GlossaryService(settings, logger, cache, hostEnvironment)
    {
        protected override List<string> EnumerateTermFiles(string directory)
        {
            throw new IOException("Simulated enumeration failure.");
        }
    }
}