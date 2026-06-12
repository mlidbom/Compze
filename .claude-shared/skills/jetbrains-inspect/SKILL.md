---
name: jetbrains-inspect
description: >-
  Headless JetBrains code inspections for findings the IDE MCP can't give you: the sub-warning tier
  (suggestions / style hints like "use var", "can be made static"), whole-solution scans, no-IDE / CI runs,
  and non-.NET languages. FIRST check the fast path — for .NET errors + warnings on specific files, use the
  Rider MCP directly (`lint_files` or `get_file_problems errorsOnly:false`, Rider 2026.2+); it's live and
  needs no temp files, so don't reach for this slower tool just for warnings. Use this skill when you need
  severities BELOW warning, the whole solution at once, no running IDE, or a non-.NET language (Python /
  JS-TS / Java-Kotlin / Go / SQL via that IDE's `inspect` CLI; .NET via `jb inspectcode`). Not for:
  navigation (LSP tool) or refactor/build (IDE MCP).
---

# JetBrains code inspection (headless, all severities)

## First — do you even need this?

**.NET errors + warnings on specific files are faster from the Rider MCP** — live from the IDE, no temp
files, no solution load:
- `lint_files` (batch; `min_severity: warning`) or `get_file_problems` (`errorsOnly: false`).
- Requires Rider **2026.2+** (the EAP added the warning tier). On 2026.1.x `get_file_problems` is
  errors-only and `lint_files` doesn't exist — then you do need `jb inspectcode` below for warnings.

**Reach for this skill only when the MCP can't cover it:**
- **Suggestions / style hints** — the tier *below* warning ("use var", "can be made static", naming
  conventions, redundancies). The MCP floors at warning; `jb inspectcode` reaches `SUGGESTION` / `HINT`.
- **Whole-solution** sweeps, a **no-running-IDE / CI** run, or a **non-.NET language**.

## Engines (by language)

| Language(s) | Engine | Tool |
|---|---|---|
| C# / VB / .NET | ReSharper | `jb inspectcode` (ReSharper Global Tools) — *but prefer the Rider MCP for errors+warnings* |
| Python, JS/TS, Java/Kotlin, Go, PHP, SQL, … | IntelliJ-platform | `<IDE>\bin\inspect.bat` |

`jb inspectcode` is .NET-only. Severity coverage is governed by the inspection profile (`.editorconfig` /
`.DotSettings` for ReSharper; `.idea/inspectionProfiles/*.xml` for IntelliJ).

## Detect what's available

```powershell
Get-Command jb -ErrorAction SilentlyContinue            # .NET (ReSharper Global Tools)
Get-ChildItem "$env:LOCALAPPDATA\Programs\*\bin\inspect.bat","C:\Program Files\JetBrains\*\bin\inspect.bat" -EA SilentlyContinue
Get-Command qodana -ErrorAction SilentlyContinue        # optional unified engine (below)
```
If `jb` is missing: `dotnet tool install -g JetBrains.ReSharper.GlobalTools`. IDE `inspect.bat`s ship with
each installed IDE. Map language → IDE: Python/JS/web→PyCharm, Java/Kotlin→IntelliJ IDEA, Go→GoLand,
SQL→DataGrip, .NET→Rider.

---

## Engine A — .NET via `jb inspectcode`  (suggestions/hints, whole-solution, CI)

For errors+warnings prefer the Rider MCP (above). Use this for the **sub-warning tier**, **whole-solution**,
or **no-IDE** runs.

```powershell
$sln = "<repo>\Your.slnx"           # *.slnx / *.sln (skip legacy/sample solutions)
$out = "$env:TEMP\inspect-$(Get-Random).xml"
jb inspectcode $sln --project="<ProjName>" --include="**/<File>.cs" `
   --severity=SUGGESTION --no-build --no-updates --format=Xml "--output=$out"
```
Flags (easy to get wrong): values need **`=`** (`-o file` fails); `--severity` default `SUGGESTION`
(`=HINT` includes the "use var" tier, `=WARNING` less noise, `=ERROR` errors only); `--no-build` reuses the
build and avoids locking a running `.exe`; `--project` matches by wildcard **prefix** (`Foo` also matches
`Foo.Tests`); omit `--include` to scan the whole project. Parse (severity is on `IssueType`, keyed by `Id`):

```powershell
[xml]$xml = Get-Content $out -Raw
$sev = @{}; foreach ($t in $xml.Report.IssueTypes.IssueType) { $sev[$t.Id] = $t.Severity }
$rows = foreach ($p in $xml.Report.Issues.Project) { foreach ($i in $p.Issue) {
  [pscustomobject]@{ Severity=$sev[$i.TypeId]; Loc="$($i.File):$($i.Line)"; Type=$i.TypeId; Message=$i.Message } } }
$rows | Group-Object Severity | Select-Object Count, Name | Format-Table -AutoSize
$rows | Sort-Object Severity, Loc | Format-Table -AutoSize -Wrap
Remove-Item $out -Force
```

---

## Engine B — other languages via IDE `inspect.bat`  (documented; smoke-test on first use)

```powershell
$ide  = "$env:LOCALAPPDATA\Programs\PyCharm\bin\inspect.bat"   # or GoLand / IDEA / DataGrip
$proj = "<repo>"                                               # project root (ideally has .idea/)
$prof = "$proj\.idea\inspectionProfiles\Project_Default.xml"   # required — see note
$dir  = "$env:TEMP\inspect-$(Get-Random)"
& $ide $proj $prof $dir -format sarif -v1 -d "relative/subdir"   # -d scopes to a directory (no per-file flag)
```
- **Profile is required** and controls which inspections/severities are reported. Prefer the project's
  `.idea/inspectionProfiles/Project_Default.xml`; if none, export one from the IDE (Settings → Editor →
  Inspections → ⚙ → Export) and reuse it. There is no "all inspections" flag — the profile defines coverage.
- **Don't run with the project open** in that IDE (lock conflicts); inspect.bat spins up its own headless
  instance.
- Scope with `-d <subdir>` (relative to project). Output is a SARIF file in `$dir` (`-format sarif`) or
  per-inspection XML (`-format xml`). Parse SARIF generically:

```powershell
$sarif = Get-ChildItem $dir -Recurse -Include *.sarif,*.sarif.json | Select-Object -First 1
$j = Get-Content $sarif.FullName -Raw | ConvertFrom-Json
$rows = foreach ($r in $j.runs[0].results) {
  $pl = $r.locations[0].physicalLocation
  [pscustomobject]@{ Severity=$r.level; Loc="$($pl.artifactLocation.uri):$($pl.region.startLine)"; Rule=$r.ruleId; Message=$r.message.text } }
$rows | Group-Object Severity | Select-Object Count, Name | Format-Table -AutoSize
$rows | Sort-Object Severity, Loc | Format-Table -AutoSize -Wrap
Remove-Item $dir -Recurse -Force
```
SARIF `level` is `error` / `warning` / `note` (IntelliJ "weak warning" + info collapse to `note`).

---

## Optional — Qodana (one unified engine, all languages)

If you want a single consistent path across many languages instead of per-IDE `inspect.bat`, JetBrains'
**Qodana** (`qodana scan`, SARIF output, per-language images/linters) is the CI-grade unified option. It
needs Docker or the native `qodana` CLI, so it's heavier — reach for it only when the per-engine routing
above is awkward (e.g. polyglot repos in CI).

## Present

Group by severity (ERROR → WARNING → SUGGESTION/note → HINT); show `Severity  file:line  rule  message`.
**Default to scoping at the file(s)/dir you're working on** — a whole-project scan is often hundreds of
findings dominated by low-signal noise (e.g. naming inspections on generated/interop code). Filter the
parsed `$rows` by `Rule`/`Type` rather than re-running. First run on a cold project is slower (it loads the
project model); scoping keeps it snappy. Always delete the temp report/dir when done.
