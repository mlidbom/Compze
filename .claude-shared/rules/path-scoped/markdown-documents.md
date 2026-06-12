---
paths:
  - "**/*.md"
---

# Markdown Document Rules

## Writing style
- **Be concise.** State conclusions, not the full reasoning journey that led to them. If a decision was reached, document the decision — not the entire debate.
- **No historical baggage.** When updating a document, rewrite it to reflect the current state. Don't append corrections or "new in v2" sections — just write what's true now.
- **No redundancy.** Don't explain something that's obvious from context. If two things are separate codebases, don't also explain that they don't share transport/dispatch/etc.
- **When editing an existing document, check if it has grown bloated.** If so, tighten it rather than just appending.

## dev_docs folder

Folder where we should place .md files about:

- **Code review findings** — issues, bugs, and design concerns discovered during reviews
- **Progress tracking** — status documents for major features and refactoring efforts
- **Architecture notes** — current-state analysis and design decisions under consideration

These documents capture the current state of the codebase and ongoing work. They are living documents meant to be updated or removed as issues are resolved and work progresses.
