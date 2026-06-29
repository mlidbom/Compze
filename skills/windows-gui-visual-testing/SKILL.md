---
name: windows-gui-visual-testing
description: >-
  Autonomously verify what a Windows desktop GUI app you are developing (WPF, WinForms, Win32) actually
  RENDERS — without Computer Use. Instrument the app itself with a self-capture channel (a trigger file
  makes the app screenshot its OWN window to a PNG you read), plus a file log, topmost-and-foreground pinning, a
  single-instance guard, and a tight edit→build→launch→capture→read loop. Use this whenever you are
  iterating on a Windows desktop app and need to SEE its rendered output — a window's pixels, layout, a
  visual bug, animation or liveness, or DWM/DirectComposition/Direct3D/GPU-composited content such as live
  thumbnails, video, or custom drawing — especially when Computer Use is too slow, masks ungranted windows,
  or gets occluded. Strongly prefer it over Computer Use for any local build-and-look loop on a native
  Windows GUI you are building; it lets you iterate solo at build speed instead of one screenshot per
  round-trip. Not for: CI or packaging builds, diagnosing a crash from a stack trace, unit or logic tests,
  or screenshotting the whole desktop, a web page, or apps you are not developing.
---

# Windows GUI visual testing without Computer Use

## Why this exists

When you change a Windows desktop app and need to see the result, the obvious tool is Computer Use. But for
a tight iteration loop it fights you:

- **One screenshot per round-trip** — slow, and you burn turns driving the mouse instead of thinking.
- **It masks ungranted windows** — its screenshots hide apps you haven't been granted, including the very
  window you're rendering *into* a preview, so live thumbnails go black and you misjudge a working feature
  as broken.
- **Occlusion** — whatever is in front (often the Claude app you're typing into) covers the target window,
  and the screenshot captures *that* instead.

The fix is to stop pointing a camera at the screen and instead **instrument the app to report on itself**.
The app writes its own composited pixels to a PNG and its own flow/errors to a log file; you drive it with
trigger files from the shell and `Read` the results. Now the loop is `edit → build → launch → capture →
Read png+log`, all at build speed, fully under your control. This single change is what turns visual
verification from a slog into something you can iterate on dozens of times in a session.

## The loop

```
edit code → build → (re)launch detached → trigger a capture → Read capture.png (+ the log) → repeat
```

Add a frame-diff step when you're checking motion ("is this actually live, or a frozen frame?").

## Find the project's harness binding (per project)

Each consuming project wires the harness into its own app and records that binding in
**`dev_docs/agent-harness.md`**: where the harness source lives, which startup flag enables it, and any
app-specific capture details. **Read that doc first** — it tells you how THIS app is wired and spares you
re-deriving (or worse, re-adding) the plumbing.

If the project has no binding doc, the harness isn't wired yet. Wire it: copy
[reference/AgentHarness.cs](reference/AgentHarness.cs) into the app (adapt the namespace), gate it behind a
dedicated startup flag (e.g. `--harness`) so the normal launch is unaffected, then write
`dev_docs/agent-harness.md` recording the binding. The three responsibilities the harness owns:

1. **Single instance** — `AgentHarness.EnsureSingleInstance("<AppName>")`, first thing in the harness
   startup path. Without it, every relaunch (and every `open_application`) spawns *another* window; you end
   up with several instances fighting over the same job and can't tell which one you're looking at.

2. **Capture pump** — `AgentHarness.StartCapturePump(window, <bounds>)`. Capture the region the window
   actually spans: the whole virtual screen for an overlay app, the window's own rectangle for a normal one.

3. **Log liberally** — `AgentHarness.Log($"hr=0x{hr:X8} ...");` anywhere. A detached app has **no visible
   Debug output**, so for native interop / HRESULTs / control flow this file log is your only window into
   what happened. Read it after every run.

## Capturing (each iteration)

The app is driven by two files next to the exe: it watches for `capture.trigger`, and writes `capture.png`.

- Easiest: `scripts/capture.ps1 -Dir <exeDir>` — removes the old png, drops the trigger, waits for the new
  png, prints its path. Then `Read` that path.
- Turnkey full loop: `scripts/build-run-capture.ps1 -Csproj <proj> -Exe <exe>` — stops the old instance,
  builds, relaunches detached with the harness flag (default `--harness`; override via `-AppArgs`, which
  also takes any extra app flags), captures, and tails the log.

**Trigger protocol (important):** create `capture.trigger`, then wait for `capture.png` to (re)appear — that
appearance is the "done" signal. Don't read the png before then or you'll get a stale/half-written frame.

## Why a screen BitBlt (not PrintWindow / RenderTargetBitmap)

`AgentHarness` captures the window's **on-screen rectangle** via `BitBlt` from the screen DC. That's
deliberate: DWM/DirectComposition/Direct3D/video content (live thumbnails, GPU overlays, hardware video) is
**not** part of the window's own GDI paint or WPF visual tree — `PrintWindow` and `RenderTargetBitmap` would
return a blank or wrong frame for exactly the content you most want to verify. Only a screen-region grab
sees the real composited result. Because that grab is occlusion-prone, the harness first calls `PinTopmostAndBringToForeground`
(`AttachThreadInput` + `SetWindowPos(HWND_TOPMOST)` + `BringWindowToTop` + `SetForegroundWindow`) so nothing
covers the window at capture time. (Trade-off: this briefly steals the foreground — fine for tests, just expect it.)

## Checking liveness / animation

A still frame can't tell you whether something is *updating*. Capture two frames a second or two apart and
diff them:

```
scripts/frame_diff.ps1 -A frame1.png -B frame2.png
```

It prints the count of changed pixels and their **bounding box**, and can crop that region. A localized
cluster of change means that area is genuinely animating; near-zero change means it's static/frozen. This is
how you distinguish "live video" from "last rendered frame," and how you localize *which* part moved when
the whole window looks similar.

## Gotchas worth remembering

- **Capture the top-level window**, even if rendering happens in a child HWND (e.g. a DirectComposition
  child). The composited result lands on screen at the top-level window's rectangle; that's what BitBlt
  reads.
- **Two-phase capture latency:** the harness raises the window on the tick it sees the trigger, then grabs
  on the *next* tick, giving DWM a frame to composite it on top. So a capture takes ~250 ms, not instant.
- **Diff false positives** from your own UI: a clock/spinner/caret in the app will show up as "change."
  Look at *where* the change is (the bbox), not just the count.
- If a capture unexpectedly shows a *different* app, it's occlusion that `PinTopmostAndBringToForeground` didn't win (e.g. the
  target wasn't the foreground process's owner). Re-check the window handle you're capturing.

## Files

- `reference/AgentHarness.cs` — reference implementation of the harness: file log, single-instance guard,
  force-to-top, and the trigger-driven self-capture (BitBlt → PNG). Copy it into the app you are
  instrumenting and adapt its UI-framework types; the copy living inside each project is the one its app
  actually runs (the project's `dev_docs/agent-harness.md` says where).
- `scripts/capture.ps1` — drive one capture against an already-running app.
- `scripts/build-run-capture.ps1` — stop + build + relaunch detached + capture + tail log, in one call.
- `scripts/frame_diff.ps1` — diff two frames (changed-pixel count, bounding box, optional crop) for
  liveness/animation checks.
