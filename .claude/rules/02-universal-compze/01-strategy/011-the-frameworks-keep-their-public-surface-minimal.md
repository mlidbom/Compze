# Compze's frameworks keep their public surface minimal; its utility libraries may be generous

Teventive, Tessaging, the tevent store and Compze's other frameworks are complex, and change how systems can be built.
For them, less is more: Compze builds the hard parts, and exposes them so that consumers can easily do the easy parts
themselves, without Compze in the way. Their interfaces stay minimal, and stay C# interfaces, so that anything behind
them can change: a consumer doesn't know the implementing types.

A public member is hard to take back once consumers use it. Had thousands of consumers relied on the order the bulk
reader once gave, before we found that sorting at every call could cost more than what they did with the result,
removing it would have broken them all. So a convenience that consumers can build themselves stays out of a framework.

Compze.Internals.SystemCE is on purpose the opposite: small conveniences, such as the `*Ordinal` string methods, with
almost no dependencies between them, which cost next to nothing to maintain and are unlikely to surprise anyone. There a
convenience that reads better than the BCL's is welcome: see
[Compze has only what improves on the BCL](001-compze-has-only-what-improves-on-the-bcl.md).
