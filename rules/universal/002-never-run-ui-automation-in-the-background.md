# NEVER run UI-automation / desktop-driving tests as background tasks — foreground, always

**Absolute rule, no exceptions:** any test or script that drives the real desktop — injects keyboard/mouse
input, switches virtual desktops, takes the foreground, drives windows — MUST be run in the **foreground**
(a blocking call you wait on). **NEVER** start one with `run_in_background`, a detached process, or anything
that returns control to you while it keeps driving the screen.

This covers (non-exhaustively): the **focus-restore regression gate**
(`labs/FocusRestoreLab/run-focus-restore-regression-tests.ps1`), anything using **windows-mcp**, **FlaUI** /
visual GUI tests, the Deskmancer focus/switch harnesses, the Hyper-V-matrix drivers run locally — any run that
hijacks input or jumps between desktops.

## Why this is non-negotiable (it has gone wrong ~10 times)

Backgrounding such a run is a **disaster** for the human at the machine:

- The **completion/notification popup poisons the result** — it steals the foreground the test is measuring.
- The notification **pulls the user back to click and read**, and that click **also poisons the result**.
- A borked run **cannot be stopped**: it is constantly jumping between desktops and stealing focus, so the
  user can't click anything to terminate it. The **only** way out is **logging out of the entire Windows
  session** — losing all their other work.

So a backgrounded UI run is worse than useless: it can't produce a trustworthy result AND it traps the user.

## What to do instead

- Run it **foreground, blocking**, and wait for it to finish before doing anything else.
- If a single foreground call would exceed the tool's timeout, **split the slow build out**: run the build as
  its own foreground step, then run the test itself with its skip-build flag (e.g. `-SkipBuild`) so the
  driving phase fits one foreground call. Do **not** reach for backgrounding to dodge the timeout.
- Don't fire other tool calls that could grab focus while it runs.

If you ever feel tempted to background one of these "so you can do other work meanwhile" — don't. There is no
other work worth poisoning the result and trapping the user for.
