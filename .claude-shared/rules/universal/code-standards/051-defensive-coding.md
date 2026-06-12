# "Defensive coding" is BANNED

**Guarantee correct state and assert it; do not check for a catalogue of corrupt states and work around
them.** A type's job is to keep itself valid and make invalid states impossible — not to defend, at every
call site, against the many ways things might have gone wrong. When you reach for a check against a state
that "shouldn't happen," stop and ask the deciding question: **could we guarantee this state with enough
effort?**

- **Yes — so do it.** If a correct state is achievable by us (a precondition our code can honor, an
  invariant we can maintain), then make it true and assert it with the project's contracts facility, so a
  violation fails loud and immediately. A *guard* here is bug-hiding — it turns our own bug into silent wrong behavior. Don't reach
  for the guard just because guaranteeing the invariant would take work; do the work.
- **No — genuinely intractable.** Only when guaranteeing the state is truly out of reach — the API doesn't
  expose what we'd need, the cost is absurd — is it a real runtime condition. Then handle it deliberately, 
  try to redesign so that we don't need to assume a particular state that we cannot guarantee, but try and do 
  so structurally, through redesign, not through countless if clauses sprinkled all over the code. 

The bar is **effort-to-guarantee, not nominal ownership.**

**Why:** every "just in case" check for an impossible state is a place a future reader must wonder "when
does this actually happen?" — and a place a real bug hides as a handled-looking no-op. Asserting instead
converts "silently cope with garbage" into "prove we never produce garbage, and stop hard the instant we
do." Exception-swallowing (below) is one specific case of this same rule.

## Exceptions: don't swallow them

**Never preemptively swallow exceptions.** Do not write a catch block "just in case" or "because the called
code might throw." A catch is only justified when both of these hold: (1) we have **empirical evidence** of
the specific exception in real usage *and* characterized it as non-bug, AND (2) there is **no other option**
— we cannot recover or surface the failure any other way. If either condition fails, delete the catch and
let the exception propagate. If it crashes, we now have empirical evidence and can add a narrow, justified
catch.

**Why:** defensive catches turn bugs into silent data corruption. `try { ... } catch { return null; }` makes
"we have a bug producing unexpected exceptions" indistinguishable from "the operation legitimately returned
no result." Downstream code treats the null as valid, the bug stays hidden, and the original cause becomes
nearly impossible to track down.

**Wide catches that look narrow are the same problem.** `catch (Exception)` is the obvious case. But
`catch (COMException)` catches every COM failure including bugs surfacing through COM. `catch (IOException)`
catches every file/network failure ever. If the catch type isn't narrow enough to exclude bug-shaped
failures, it's still bug-hiding. Truly-narrow catches are filtered by HRESULT or error code
(`catch (COMException) when (ex.HResult == specific)`) and justified by observation.

**Logging is not a substitute for not catching.** Logged-and-swallowed is still swallowed — the caller can't
tell it happened, the calling code that depends on the success path silently degrades.

**When in doubt, delete the catch.**
