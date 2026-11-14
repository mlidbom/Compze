# Go through Compze for what the BCL does as well

Agreed with Magnus 2026-11-14 as a task to do later; not started.

The rule "Compze has only what improves on the BCL"
(`.claude/rules/02-universal-compze/01-strategy/001-compze-has-only-what-improves-on-the-bcl.md`) says a Compze type or
member is worth having only where it improves on what .NET's base class library offers, and to recommend removing what the
BCL does as well. Compze may have such things because the BCL gained them since, or because they were written without
knowing the BCL had them. Nothing has been gone through for them yet, and finding them only by coming across them is slow.

## The task

- Go through Compze's public types and members, package by package, and list those the BCL does as well: each with the
  BCL type or member that does it, and the .NET version that brought it.
- A better, more intuitive API is improvement enough: keep what reads better than the BCL, even when it does nothing more,
  as `StartsWithOrdinal` and the other `*Ordinal` string methods in Compze.Internals.SystemCE do.
- Report the candidates to Magnus, and remove nothing without his word: they're public API.
- Start where general-purpose helpers collect, Compze.Internals.SystemCE and Compze.Threading, and with what .NET 9, 10
  and 11 added.
