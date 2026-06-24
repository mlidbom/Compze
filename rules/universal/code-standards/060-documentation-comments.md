# Documentation comments `///` comments

This file is long on purpose. Doc comments are the one thing agents fail at over and over, every time for
the same reason. Read all of it. The *why* is in here, not just the letter — if you only follow the letter
you will regress to the failure the moment a case isn't spelled out.

## Audience Write for someone that does NOT know the context
Doc comments are written for those that do NOT already know what the thing is.
They Are written for someone that 
- **They do NOT know this codebase**
- **Do NOT know the specialized substrate the code is built on** — native OS APIs and interop, COM, wire protocols, vendor SDKs, undocumented internals.

The reader is not there for your clever caveats. They are there for "what is this thing."

## Use linking tags for every instance of a word that refers to something in our code

Anything that refers to a symbol that can be referenced by `<see cref="…"/>`, `<paramref name="…"/>`, `<typeparamref name="…"/>` etc. MUST be. Fall back to `<c>…</c>` if the symbol is in a non-referenced assembly but NEVER leave it as just a word.

Why: 
- Each tag becomes a real link that the developer can follow to explore the code. This is invaluable to me.
- Renaming symbols using the MCP tools automatically renames every reference, making renaming incomparably easier

Note: Words that describe something in our code's domain that cannot be linked are a serious code smell. It implies that we have an important concept that has no code representation. This is a serious problem in DDD. Report it, don't ignore it

**Never ignore warnings about <see> tags etc. pointing at something unresolvable. Always fix it.**

## Structure:

### `<summary>` — the newbie floor (the WHAT)

"What is this thing, to someone who has never heard of it, and what is it for." A handful of sentences at most for most types.

### First `<remarks>` tags, if required
Why does this exist; why is it needed; what breaks without it.

### Second `<remarks>` tag, if required
Exact mechanics/mechanism, caveats: "PrintWindow returns black for DWM-composited content"


## Do not write this in doc comments:
- **Reformulation of the signature of a member**. If that is all that seems fit to put in the comment. Remove the comment.
- **Any claims that something is settled forever and should not change**
- 
## How to formulate
- **The explanation you'd give in chat IS how to formulate the comments.**

## Reasonably short lines
- A long single-line is hard to read — break it across lines with <br/> so that the formatting is good both in the code and when viewed in the IDE hover displays.

## Which members get a comment — and why the length is free
- **Document every member unless it is trivial *by its name to someone NOT familiar with the code and concepts***

The IDE collapses every doc comment to its **first line** until you hover or expand it. The worst case is a line a reader finds obvious
and skips in two seconds. A missing line on a boundary verb they don't know is the expensive failure — so when
unsure, write it.
