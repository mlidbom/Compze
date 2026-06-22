# Warnings: fix the problem, never silence the messenger

**Fixing warnings well is always welcome.** The failure mode this rule exists to stop is *suppressing*
warnings instead of fixing what they point at — pragma disables, `// ReSharper disable`, attribute
suppressions, severity downgrades, or a lazy refactor whose only purpose is to dodge the analyzer.

- The default response to a warning: understand what it is telling you and fix the real problem.
- Suppression is the rare exception, justified only when the inspection genuinely doesn't fit the site —
  never because the proper fix is work. If you believe a suppression is right, propose it and let the user
  decide.
- Every suppression carries its rationale **on the same line as the directive** — never on a separate
  comment line above it:
  - ReSharper: `// ReSharper disable once SomeInspection <rationale>` (same for the block form).
  - Roslyn/CA: `#pragma warning disable CAxxxx // <rationale>`.
- **Never enable `<TreatWarningsAsErrors>`.** Errors-on-warnings pressures contributors and AIs toward
  whatever silences the analyzer fastest to get a build through — it breeds exactly the suppressions this
  rule bans. Warnings staying warnings leaves room to fix them properly.
