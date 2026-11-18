# Public types are designed for every consumer, never for the call sites in front of you

Compze's libraries are public. Whatever is public is used by code we can't see: other repositories, other people's
applications. So:

- **Weigh seriously whether what a consumer asks for belongs in a library for general use.** A consumer's request, such
  as JAStudio's agent asking for a method, an overload or a guarantee, shows that it has a need, not what Compze's API
  should be. Would Compze have it for consumers we don't know? Could the consumer build it on what Compze already
  offers, and would its own code then be better than with our shortcut? Many requests belong in Compze; none is added
  because it is convenient for one consumer now. Magnus, 2026-11-16, on what to catch: "kitchen sink style types and/or
  methods crammed in to make JAStudio's life a bit easier, or even just to prevent refactorings in JAStudio that would
  actually result in better code than the shortcuts our added stuff gave them." Compze.Internals.SystemCE is the
  exception: see [the frameworks keep their public surface minimal](011-the-frameworks-keep-their-public-surface-minimal.md).
- **Never judge a public type's guarantees, semantics or completeness by how Compze itself uses it.** "Nothing in
  Compze needs X" is no argument about public API: the callers that matter aren't in this repository. Surveying
  Compze's call sites tells you what Compze needs, never what a consumer may rely on.
- **A public type keeps the semantics its name, and its .NET counterpart, lead a consumer to expect,** unless its name
  and documentation say otherwise. `LazyCE` runs its factory once, as `Lazy<T>` does by default; a cheaper variant
  that may run it twice would need a name that says so.
- **YAGNI still decides whether a public member exists at all.** A consumer that would call a member shows that it would
  be called, not that it belongs in Compze. Once it exists, it is complete and unsurprising for any consumer: no gaps
  that only Compze's own usage happens to avoid.
- **This includes the published Internals packages,** such as Compze.Internals.SystemCE. Their APIs may change faster,
  but what they make public is designed to this same standard.

Compze's own call sites are evidence only for internal code.

Pairs with [minimize-public-types-and-members](010-minimize-public-types-and-members.md): that rule decides what is
public; this one decides how well.
