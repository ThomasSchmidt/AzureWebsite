using System.Collections.Generic;
using System.Threading.Tasks;
using AzureWebsite.Models.Domain;

namespace AzureWebsite.Services;

/// <summary>
/// Service interface for glossary term management.
/// Handles discovery, parsing, and caching of markdown glossary terms.
/// </summary>
public interface IGlossaryService
{
    /// <summary>
    /// Retrieves all glossary terms, sorted alphabetically by name (case-insensitive).
    /// </summary>
    Task<IEnumerable<GlossaryTerm>> GetAllTermsAsync();
}