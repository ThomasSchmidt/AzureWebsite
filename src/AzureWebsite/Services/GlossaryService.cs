using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureWebsite.Models.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureWebsite.Services;

/// <summary>
/// Configuration settings for the glossary feature.
/// </summary>
public class GlossarySettings
{
    /// <summary>
    /// Directory containing markdown glossary term files (relative to content root).
    /// </summary>
    public string TermsDirectory { get; set; } = "Data/glossary";
}

/// <summary>
/// Service for discovering, parsing, and caching glossary terms from markdown files.
/// </summary>
public class GlossaryService : IGlossaryService
{
    private readonly string _termsDirectory;
    private readonly ILogger<GlossaryService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _cacheKeyTerms = "glossary_all_terms";

    public GlossaryService(IOptions<GlossarySettings> settings, ILogger<GlossaryService> logger, IMemoryCache cache)
    {
        _termsDirectory = settings.Value.TermsDirectory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<GlossaryTerm>> GetAllTermsAsync()
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<GlossaryTerm>>(_cacheKeyTerms, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var terms = new List<GlossaryTerm>();
            var directory = _termsDirectory;

            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Glossary terms directory not found: {Directory}", directory);
                return terms;
            }

            var files = Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly).ToList();

            foreach (var file in files)
            {
                try
                {
                    var term = await ParseTermFileAsync(file);
                    terms.Add(term);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse glossary term: {File}", file);
                }
            }

            terms.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return terms;
        });
        return result!;
    }

    private static async Task<GlossaryTerm> ParseTermFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        var (name, description) = ExtractFrontmatter(content, fileName);

        return new GlossaryTerm
        {
            Name = name,
            Description = description
        };
    }

    private static (string Name, string Description) ExtractFrontmatter(string content, string fileName)
    {
        // Normalize line endings and split into lines so the opening/closing "---"
        // markers are only recognized when they appear alone on their own line —
        // this avoids a "---" inside a quoted frontmatter value (or in the body)
        // being mistaken for the closing delimiter.
        var lines = content.Replace("\r\n", "\n").Split('\n');

        if (lines[0].Trim() != "---")
        {
            // No frontmatter — treat entire content as the description.
            return (fileName, content.Trim());
        }

        var closingLineIndex = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                closingLineIndex = i;
                break;
            }
        }

        if (closingLineIndex == -1)
        {
            // Opening "---" found but no closing marker on its own line — treat remainder as plain description.
            return (fileName, content.Trim());
        }

        var yaml = string.Join('\n', lines[1..closingLineIndex]).Trim();
        var body = string.Join('\n', lines[(closingLineIndex + 1)..]).Trim();
        var name = ParseTermName(yaml) ?? fileName;
        return (name, body);
    }

    private static string? ParseTermName(string yaml)
    {
        foreach (var line in yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
                continue;

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex == -1)
                continue;

            var key = trimmed[..colonIndex].Trim().ToLowerInvariant();
            var value = trimmed[(colonIndex + 1)..].Trim();

            // Remove surrounding quotes
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            if (key == "term")
            {
                return value;
            }
        }

        return null;
    }
}