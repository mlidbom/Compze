# NEVER change an external environment without explicit approval — read freely, mutate only on the word

**Absolute rule, no exceptions without explicit per-change approval:** never create, update, delete, deploy,
or otherwise **mutate state in an external service or account** on your own initiative. **Inspecting and
reading is always fine; changing is not.** The moment you find yourself reaching for a tool that *writes* to
an outside account, **stop and ask first** — state the exact change you intend and wait for an explicit
go-ahead.

This covers (non-exhaustively) every service/account that lives outside the local working tree:
GitHub, PostHog, Cloudflare, Polar, Azure, Google Workspace, package registries (npm / NuGet), and any other cloud / SaaS / hosting / payment / DNS / email account** — the same.
