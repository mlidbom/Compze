# Example — a 245-line cache god-class → orchestration + rehomed collaborators

**Diff:** [affinity-cache-god-class-split.diff](affinity-cache-god-class-split.diff)

**What it teaches:** how to take one class that holds several abstraction levels at once and split it so the
class keeps *one* responsibility and delegates every other concept to a collaborator — creating the
collaborator when none exists. The centrepiece is a static cache class that shrinks from **245 lines to 78**,
not by deleting behaviour but by **moving each non-cache concept to where it conceptually belongs**.

> Read the diff top-to-bottom once, then this walkthrough. The lesson is the *pattern of moves*, not the
> Win32 specifics.

---

## The before — what made it hard to read

`WindowDesktopAffinityCache` (the `a/` side of the diff) was one static class that mashed together **four
different abstraction levels**. To understand *any* line you had to hold *all four* in your head at once:

1. **Cache/staleness policy** — a `Stopwatch`, `_valuesRefreshedAt`, `ValuesFresh`, the "re-resolve the whole
   set past 50 ms" rule.
2. **The Win32 + COM affinity-resolution mechanism** — `GetWindowDesktopId`, the all-desktops sentinel GUID,
   the `TYPE_E_ELEMENTNOTFOUND` HRESULT, the undocumented `GetWindowBand` band read, the `GW_OWNER`
   owner-chain walk, the `IVirtualDesktopManager` COM object.
3. **A threading idiom** — `Task.Run(f).GetAwaiter().GetResult()` inlined, to get the COM off an
   input-synchronous hook thread.
4. **Dictionary mechanics** — hand-rolled "remove keys not in the live set" and "merge these entries" loops.

The class is named for concept #1, so #1 is the responsibility it should *keep*. #2, #3 and #4 are guests.
That is the whole diagnosis. Everything below is eviction.

## The moves — each guest rehomed to where it belongs

Follow these in the diff. Each is a small, verifiable move; none is "clever".

| Guest concept | New home (why there) |
|---|---|
| Staleness policy (`Stopwatch`, `_valuesRefreshedAt`, `ValuesFresh`, mark-refreshed) | **`CacheExpirationHelper`** — a new class. It has its own state and a self-contained rule, so it earns a type. Note `RefreshIfStale(Action)` *encapsulates marking-refreshed-afterwards*, so the cache **cannot forget** to mark it. |
| The whole COM/band/owner affinity mechanism | **`HWND` extension members** (`HWNDEX_DesktopAffinity.cs`): `GetEffectiveAffinity`, `GetDesktopAffinity`, `FollowsEveryDesktopByBand`, `RootOwner`, `Band`, `GetDesktopId`. Behaviour goes **onto the data it operates on** — a handle. It now reads `handle.GetEffectiveAffinity()`, discoverable by typing `.`. |
| The magic constants (sentinel GUID, HRESULT, the COM object) | They **travel with the mechanism** into `HWNDEX_DesktopAffinity`, not left behind. A mechanism owns its own magic. |
| Win32-class → role classification (`"Shell_TrayWnd"`, `"Progman"`, …) | **`SafeHwndEX.Kind`** — the magic class atoms confined to one place. |
| "wrap a bare handle so its operations are reachable" | **`HwndEX.ToSafeHandle`** (renamed home of the old `HwndExtensions`). |
| `Task.Run(f).GetAwaiter().GetResult()` | **`TaskEX.RunSync`** — the incantation named once; the *why* stays a comment at the call site. |
| "remove keys not in set" / "merge entries" loops | **`DictionaryEX.RemoveAllBut` / `Merge`** — now `AffinityByHandle.RemoveAllBut(live)` reads as English. |
| pipe-forward so orchestration reads as a pipeline | **`PipeEX._`**: `candidates ._(resolve) ._(merge)`. |

Two moves are pure **truthfulness**, not extraction — and they matter just as much:

- **`WindowKind.Application` → `WindowKind.UnknownPotentiallyApplication`.** The Win32 class can only tell you
  a window is a *candidate*, never that it truly is an application. The old name lied; the rename tells the
  truth. (Renaming is the highest-leverage fix there is — see `012`.)
- **`DesktopAffinity.Of(ManagedWindow)`** absorbs the never-null assertion. The "a managed window always has
  an affinity" invariant now lives on the affinity type, so `ManagedWindow.DesktopAffinity` is a one-liner.

## The residue — what "single responsibility" looks like

After the moves, the cache body is pure orchestration at **one** level (read the `b/` side):

```
RegisterCurrentlyLiveWindows:  forget the gone → resolve the new candidates in one hop → merge.
OfHandle:                      refresh if stale → get-or-add.
Invalidate:                    mark expired.
```

Every verb is a call to something named for what it does. You can now understand the **caching strategy**
without knowing a single thing about COM, bands, HRESULTs, or owner chains — each is a black box behind a
truthful name you may choose not to open. That is the goal: **≤ 5 chunks to understand the class.**

## The behaviour-equivalence audit — do this every time

Extraction silently changes behaviour (a moved `try`/`catch` scope, a lost short-circuit). This split was
audited move-by-move; the honest result:

- **One intended edge-case change:** the owner-fallback now also checks the *owner's* band, because
  `GetEffectiveAffinity`'s owner path calls `owner.GetDesktopAffinity()` (band-first). The old owner-fallback
  only asked `GetWindowDesktopId`. Net: an owned tool-window whose owner is an all-desktops window now
  resolves to `All`. Rare, arguably more correct — but a **real difference, so it gets named**, not buried.
- **Accepted micro-change:** single-window resolves now always do one thread hop (the old code could do zero
  for a band-follower). Waved off deliberately.
- **Dead code removed, no behaviour change:** a second `HasAffinity` query method was cache-forever
  membership the registry never actually used (it minted via `Of`), so deleting it changed nothing.

The lesson: **list each behaviour of the before, confirm the after preserves it, and flag the ones you change
on purpose.** "It still builds" is necessary, not sufficient.

## Reading the diff: lesson vs. mechanical churn

The 28 files are not equally instructive. Focus your reading:

- **The lesson (read closely):** `WindowDesktopAffinityCache.cs`, `CacheExpirationHelper.cs`,
  `HWNDEX_DesktopAffinity.cs`, `SafeHwndEX.cs`, `HWNDEX.cs`, `TaskEX.cs`, `DictionaryEX.cs`, `WindowKind.cs`,
  `WindowRegistry.cs` (the mint call-site), `DesktopAffinity.cs` + `ManagedWindow.cs` (the `Of` relocation).
- **Mechanical propagation (skim):** the lab `Program.cs`/experiment files, `.csproj`, `.DotSettings`, and
  the import-only edits to `IWindow.cs` / `IOwnedWindow.cs` / `IRegistryWindow.cs` — these are just the
  rename and the new packages rippling out. Real, but they teach nothing about the split.

## The standards this embodies

`031` everything-in-its-place (each concept gets its own home) · `014` rehome-concepts-first (give the
scattered concepts truthful homes *before* touching wiring — once the homes exist the split falls out) ·
`030` data + logic together (the mechanism moves onto the `HWND` it acts on) · `010`/`013` naming (understand
by names alone) · `005` the 5–7 slot mind (one concept per file so working memory never overflows).
