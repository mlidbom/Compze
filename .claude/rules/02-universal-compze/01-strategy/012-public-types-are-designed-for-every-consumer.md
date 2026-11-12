# Public types are designed for every consumer, never for Compze's own call sites

Compze's libraries are public. Whatever is public is used by code we can't see: other repositories, other people's
applications. So:

- **Never judge a public type's guarantees, semantics or completeness by how Compze itself uses it.** "Nothing in
  Compze needs X" is no argument about public API: the callers that matter aren't in this repository. Surveying
  Compze's call sites tells you what Compze needs, never what a consumer may rely on.
- **A public type keeps the semantics its name, and its .NET counterpart, lead a consumer to expect,** unless its name
  and documentation say otherwise. `LazyCE` runs its factory once, as `Lazy<T>` does by default; a cheaper variant
  that may run it twice would need a name that says so.
- **YAGNI still decides whether a public member exists at all.** Once it exists, it is complete and unsurprising for
  any consumer: no gaps that only Compze's own usage happens to avoid.
- **This includes the published Internals packages,** such as Compze.Internals.SystemCE. Their APIs may change faster,
  but what they make public is designed to this same standard.

Compze's own call sites are evidence only for internal code.

Pairs with [minimize-public-types-and-members](010-minimize-public-types-and-members.md): that rule decides what is
public; this one decides how well.
