# Don't over-apply Law of Demeter
Exposing a rich domain component, that is truly a part of what something is, is not a leak.
A taskbar has, or is, a Window. Forwarding every method is obfuscation and duplication, not encapsulation.

# Ignore DRY.
Multiple similar classes/methods with different conceptual meanings is FINE.
What must not happen is implementing the same abstraction more than once.

# YAGNI cuts speculative behavior — not abstractions, and not the right kind of type.
Extract the moment it reads clearer, reused or not — "needed only once" is no reason to inline. And model a
thing as the *kind* of thing it is *now* — an entity → a class with identity, a value → a type with value
semantics (per the language rules' guidance on which construct to use); that is a present truth, not a future
feature. YAGNI withholds only *behavior/members nothing yet calls*: right nature now, members when needed.