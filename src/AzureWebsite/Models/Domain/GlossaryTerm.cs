namespace AzureWebsite.Models.Domain;

/// <summary>
/// Represents a single glossary term and its description.
/// </summary>
public class GlossaryTerm
{
    /// <summary>
    /// The term name (e.g., "ADR").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The plain-text description of the term.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}