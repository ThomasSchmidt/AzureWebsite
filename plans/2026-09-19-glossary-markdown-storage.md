# Glossary Markdown Storage Refactor

## Purpose
Refactor the glossary feature so terms are no longer hardcoded in C#. Each glossary term is
stored as an individual markdown file, parsed into a domain model, and served through a
dedicated service layer, matching the existing Blog feature's architecture.

## Scope
- Move glossary data out of `Pages/Glossary.cshtml.cs` into markdown files.
- Introduce `GlossaryTerm` domain model in `src/AzureWebsite/Models/Domain`.
- Introduce `IGlossaryService` / `GlossaryService` in `src/AzureWebsite/Services`.
- Update `GlossaryModel` (Pages/Glossary.cshtml.cs) to use the service via DI.
- Register the service (+ options) in `Program.cs`, following the Blog pattern (IMemoryCache,
  IOptions<GlossarySettings>).
- Add markdown content files under `src/AzureWebsite/Data/glossary/` (one file per term),
  wired into the `.csproj` content glob (already covers `Data\**\*`).
- Achieve 100% code coverage (line+branch where feasible) on all new/changed production code.

## Out of scope
- Changing the rendered `/glossary` page markup/styling beyond what's needed to keep behavior identical.
- Blog feature changes.

## Acceptance criteria
1. No glossary term data remains hardcoded in C#; all terms load from markdown files at `src/AzureWebsite/Data/glossary/*.md`.
2. `GlossaryTerm` is a domain model class in `AzureWebsite.Models.Domain` namespace, file at `src/AzureWebsite/Models/Domain/GlossaryTerm.cs`.
3. A service layer (`IGlossaryService`/`GlossaryService`) reads, parses (frontmatter: `term`/`title` + body as description, or simple `name`/`description` keys), and caches glossary terms; unreadable/missing directory returns empty list without throwing.
4. `GlossaryModel.OnGet` asynchronously loads terms via the service, sorted alphabetically (case-insensitive) as before.
5. Existing `/glossary` page renders identically (term name + description columns), verified via Playwright.
6. `dotnet build AzureWebsite.slnx` succeeds with no new warnings.
7. `dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute="GeneratedCodeAttribute"` shows 100% line and branch coverage for `GlossaryTerm`, `GlossaryService`, `GlossarySettings`, and `GlossaryModel`.
8. Two seed markdown files exist reproducing current content: `adr.md` ("ADR"), `agentic-engineering.md` ("Agentic Engineering").

## Architecture decisions
- Reuse the Blog feature's pattern: `IMemoryCache`-backed service, `IOptions<GlossarySettings>` for
  configurable directory (default `Data/glossary`), manual frontmatter parsing (no need for Markdig
  since descriptions are plain text, but reuse Markdown-to-HTML only if a term description needs
  rich formatting — kept as plain text per current design, so no Markdig dependency needed for glossary body,
  just extract frontmatter values).
- Markdown file format per term (frontmatter + optional body, but since current design is name+description
  only, we store both in frontmatter for simplicity):
  ```md
  ---
  term: ADR
  ---
  Short for Architectural Decision Record, which contains information about important architectural decisions.
  ```
  - `term` (or `name`) comes from frontmatter; the body (after the closing `---`) is the description.
  - Filename does not need to match the term name (slug is not user-facing here), but we use a
    kebab-case slug of the term for readability.
- `GlossaryTerm` domain model: `public sealed class GlossaryTerm { public string Name { get; set; } public string Description { get; set; } }` (mutable POCO like `BlogPost`, consistent with codebase conventions after review — record vs class decision left to net-developer, but must live under `Models/Domain`).

## Affected files (write ownership — single worker, sequential, no parallel split needed)
- `src/AzureWebsite/Models/Domain/GlossaryTerm.cs` (new)
- `src/AzureWebsite/Services/IGlossaryService.cs` (new)
- `src/AzureWebsite/Services/GlossaryService.cs` (new)
- `src/AzureWebsite/Pages/Glossary.cshtml.cs` (modify — remove hardcoded array/record, inject service)
- `src/AzureWebsite/Pages/Glossary.cshtml` (modify only if needed for async model binding — should stay same)
- `src/AzureWebsite/Program.cs` (modify — DI registration, same section style as Blog)
- `src/AzureWebsite/Data/glossary/adr.md` (new)
- `src/AzureWebsite/Data/glossary/agentic-engineering.md` (new)
- `test/AzureWebsite.Tests/Models/GlossaryTermTests.cs` (new)
- `test/AzureWebsite.Tests/Services/GlossaryServiceTests.cs` (new)
- `test/AzureWebsite.Tests/Pages/GlossaryPageModelTests.cs` (new)

## Dependency graph
Single bounded task, sequential within one worker (net-developer). No parallel task split needed —
the change set is small and every file is interdependent (model -> service -> page model -> DI).

## Risks
- OutputCache on `OnGet` combined with async loading — must keep `[OutputCache(Duration = 300)]`
  attribute compatible with an async method signature (Blog page already proves this works).
- 100% coverage requirement means exception/error branches (missing directory, malformed frontmatter)
  must be exercised by tests.
- Must not break existing nav link/CSS (already committed in baseline) — do not modify unless required.

## Validation
- `dotnet build AzureWebsite.slnx`
- `dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute="GeneratedCodeAttribute"` + coverage report review for the 4 touched types (100% target).
- Playwright smoke check of `/glossary` rendering identical content after refactor.

## Rollback
- Revert to commit `d468d4c` (baseline hardcoded glossary) on this branch, or discard the
  `feature/glossary-markdown-storage` branch/worktree entirely; no shared/master files are touched
  until final integration merge.
