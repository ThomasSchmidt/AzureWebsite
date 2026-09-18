# Multi-Agent Feature Delivery

**Date:** 2026-09-18
**Status:** Implemented

## Goal

Enable a feature-description-to-delivery workflow in which a coordinating architect plans, delegates isolated implementation work, and keeps an independent review loop open until all confirmed actionable findings are resolved.

## Implementation

- Make `architect` the only coordinating agent, with explicit subagent access and ownership of planning, integration, review, and verification.
- Constrain `net-developer` and `frontend-developer` to separate, bounded server-side and UI tasks respectively.
- Require `net-developer` to complete a task-level `code-review` loop.
- Require `frontend-developer` to complete `ux-designer`, `accessibility-expert`, and task-level `code-review` gates in that order.
- Configure `ux-designer` as a read-only UX gate and `accessibility-expert` as a read-only WCAG 2.2 AA gate.
- Add `code-review`, configured with `GPT-5.4 (copilot)` rather than the implementation agents' `Claude Sonnet 5 (copilot)` model.
- Require `code-review` to return `ChangesRequested` until the current completed task diff or integrated diff contains no actionable findings; `architect` assigns fixes and requests a fresh review after each iteration.
- Add `security-review` as a read-only gate after integrated code review approval. Security findings use stable identifiers, return to the responsible developer, and trigger a new integrated code-review and security-review cycle.
- Make `ux-designer`, `accessibility-expert`, `code-review`, and `security-review` tool-restricted read-only gates. Validation is performed by the developers and `architect`.
- Escalate an unresolved security finding to a blocker after three complete integrated review cycles or immediately if a resolved finding regresses.
- Make `architect` the only user-invocable agent; all specialists are available only as subagents.

## Agent Configuration

| Agent | Model | Invocation | Completion condition |
|---|---|---|---|
| `architect` | Claude Sonnet 5 | User-invocable only | All acceptance criteria and required approvals are verified |
| `net-developer` | Claude Sonnet 5 | `architect` | `code-review` approval or an ownership blocker |
| `frontend-developer` | Claude Sonnet 5 | `architect` | `ux-designer`, `accessibility-expert`, and `code-review` approvals, or an ownership blocker |
| `ux-designer` | Claude Sonnet 5 | `architect` or `frontend-developer` | `UXStatus: Approved` |
| `accessibility-expert` | Claude Sonnet 5 | `architect` or `frontend-developer` | `AccessibilityStatus: Approved` |
| `code-review` | GPT-5.4 | `architect`, `net-developer`, or `frontend-developer` | `ReviewStatus: Approved` |
| `security-review` | GPT-5.4 | `architect` after integrated code review | `SecurityReviewStatus: Approved` |

## Workflow

```text
Feature description
  -> architect creates plan and task boundaries
  -> net-developer and frontend-developer implement isolated tasks
  -> net-developer -> task-level code-review -> fixes until approved
  -> frontend-developer -> ux-designer -> accessibility-expert -> task-level code-review -> fixes until approved
  -> architect integrates approved output
  -> code-review reviews integrated diff -> fixes until approved
  -> security-review reviews approved integrated diff
  -> responsible developer fixes actionable security findings
  -> code-review re-reviews updated integrated diff -> fixes until approved
  -> security-review re-reviews approved integrated diff -> fixes until approved
  -> architect reports a blocker if the same SecurityFindingId remains after three cycles or regresses
  -> architect verifies acceptance criteria after ReviewStatus: Approved and SecurityReviewStatus: Approved
```

## Validation

- Confirm each agent file uses valid YAML frontmatter and its intended role/model.
- Confirm `architect` lists every worker and reviewer in its `agents` collection.
- Confirm all specialist agents are subagent-only and all agent references resolve.
- Confirm the frontend gate order is UX, accessibility, then code review.
- Confirm security review follows integrated code review and every security fix restarts the integrated code-review and security-review loop.
- Confirm read-only gates lack edit and execution tools, and the frontend developer can read files.
- Confirm security findings have stable identifiers and the escalation threshold is documented.
