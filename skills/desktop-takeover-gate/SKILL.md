---
name: desktop-takeover-gate
description: >-
  Use BEFORE running anything that takes over the user's desktop in the foreground — a lab's experiment/takeover
  mode, a UI-automation or visual GUI test, a focus/virtual-desktop-switch harness, the Hyper-V-matrix drivers run
  locally, or an ad-hoc window-driving script you wrote. It pops a topmost, all-desktops permission prompt ("I am
  about to run X in the foreground; while it runs you can't use the computer; runs automatically in 5s") with
  [OK]/[Cancel], runs the command in the foreground, then shows a symmetric "done" notification so the user knows
  their computer is free again. Cancel returns exit code 64 — a unique signal to STOP and tell the user they
  cancelled, never retry silently. This is HOW you satisfy the never-background rule (002): never background such a
  run; run it foreground through this gate. Not for: ordinary builds, file edits, code-intelligence/MCP calls, or
  any command that does not seize the screen.
---

# Desktop-takeover gate

## Why this exists

Some runs **take over the whole computer** while they execute — they switch virtual desktops, take the foreground,
inject input, drive windows. While one runs, the user **cannot use their machine**, and a result is poisoned if
anything (a stray click, a notification) grabs focus mid-run. The standing rule ([002](../../rules/universal/002-never-run-ui-automation-in-the-background.md))
is: **never background these — run them foreground, blocking.** This gate is how you do that *humanely*: it warns
the user before seizing the screen, gives them a few seconds to veto, and tells them the moment their computer is
theirs again.

It is also the consistent entry point for **every** such run — each lab's takeover mode is launched through it, and
any throwaway desktop-driving thing you invent you run through it too.

## How to run it

Call the attached script in the **foreground** (a normal blocking Bash/PowerShell call — **never** `run_in_background`):

```
pwsh -File <this-skill>/scripts/Invoke-DesktopTakeover.ps1 `
     -Description "<human description of the task>" `
     -FilePath    "<exe or command to run>" `
     -ArgumentList <args...>
```

The script: shows the confirm prompt (topmost, on every desktop, 5-second auto-proceed countdown) → on OK runs the
command with `Start-Process -Wait` (ShellExecute, so it correctly launches **uiAccess** lab exes and gives them
their own console) → shows the "done" notice (auto-closes), even if the command crashed.

## Reading the result — this is the point of the gate

- **Exit code 64** (and the line `DESKTOP-TAKEOVER CANCELLED BY USER: <description>` on stdout): the user pressed
  Cancel. The command did **not** run. **Stop, tell the user they cancelled, and do not re-run it without asking.**
- **Any other exit code**: the command ran. That code is the *command's* (0 when it couldn't be read — judge success
  from whatever the command itself wrote, e.g. its result file, not from this code). Report the command's outcome.

## Launch each lab through it

A lab's desktop-driving mode is never launched bare — it goes through the gate. For example the Z-order battery:

```
pwsh -File <this-skill>/scripts/Invoke-DesktopTakeover.ps1 `
     -Description "the Deskmancer.ZOrderLab Z-order battery" `
     -FilePath    "C:\Program Files\DeskmancerDevelopmentVersion\<worktree>\ZOrderLab\Deskmancer.ZOrderLab.exe" `
     -ArgumentList experiment
```

## Gotchas

- **The gated command gets its own console** (ShellExecute), so its stdout does not come back to you — read the
  **file** it writes (a results `.md`, a log), not the gate's stdout.
- **Unattended runs still advance:** with no one to click, the confirm prompt auto-proceeds after the countdown and
  the done notice auto-closes — so a VM-matrix or AFK run is not stuck waiting (it does need an interactive
  window-station session; a truly headless host has no GUI to show the prompt on).
- **The prompt briefly takes the foreground** to be seen — expected, and it is shown *before* the run, not during,
  so it never poisons the measurement.

## Files

- `scripts/Invoke-DesktopTakeover.ps1` — the gate: confirm prompt → run command foreground → done notice. Exit 64
  on cancel, otherwise the command's exit code.
