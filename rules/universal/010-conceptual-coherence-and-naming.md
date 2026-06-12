# The king of all concerns: conceptual coherence.

Every type and member is one clear concept, held at a consistent level of abstraction, and its **name tells
the truth about what it is**. A type name that implies something untrue is not polish — it is a fatal design error. 
A `User` must be a user; a value object holding a user's registration details is `UserRegistrationData`, never `User`.

Because a name *is* the abstraction's interface, **renaming is the most important refactoring there is** — the
primary act that restores coherence, and a truer name is a fix to make now, never cosmetic
([renaming](012-renaming-is-the-most-important-refactoring.md)).

The operational test — you understand the code by reading names, never by decoding implementations:

- **A class:** its name, and its members' *names alone*, tell you what it is and what each member is for.
  Needing to read a member's body to learn what it is *for* is a design defect.
- **A method:** you never stop to work out what a line or section does. Anything not obvious as it stands is
  extracted behind a name that makes the caller read as plain truth — recursively, all the way down.

The how-to lives in [everything-in-its-place](03-code-standards/010-everything-in-its-place.md) and
[naming](03-code-standards/011-naming.md). This file only ranks it: it outranks everything below.

## Traps. Never trade the above for these.

### Minimalism / "YAGNI" / "don't extract until reused"
The default failure mode, and the one that most fights the goal. Extract every time it reads clearer — reuse
count is irrelevant, "used once" is no reason to inline. YAGNI withholds only speculative *behavior nothing
calls*, never an extraction-for-clarity or the right kind of type — see
[standard-wisdom-traps](03-code-standards/025-standard-wisdom-traps.md).

### Keeping a name short
A name is as long as it needs to be for instant understanding and full conceptual clarity — never shorter.
Brevity is a tie-breaker between equally clear names, never a reason to sacrifice clarity.

### Performance
Performance does not even enter the picture until an actual user notices that something feels slow.

### Avoiding large refactorings
Total restructurings are not just fine, but encouraged, as long as the resulting code reads clearer.

### Convention
Do not go against the goals and guidelines we set up because
* It is unconventional
* The current code already goes against it
