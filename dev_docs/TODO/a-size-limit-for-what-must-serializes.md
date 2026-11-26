# Compze.Must: a size limit for what an assertion's failure message serializes

Decided by Magnus 2026-11-26, passed on by tooling-with-magnus: to be done once the tooling's work on the machine's load
is done, not while the machine is loaded. Not started.

## Why

A Cloyd specification's `Must().ReferenceEqual` on a `SessionTab` failed with an `OutOfMemoryException`, instead of
saying which references differed. iteration-time found it; it hasn't been reproduced.

- `ReferenceEqual` and `NotReferenceEqual` (`Assertions/ReferenceEqualityAssertions.cs`) hand both values to
  `AssertionContextWriter.ExpressionValue`, as every assertion does, and it serializes each value whole.
- The depth is limited: `NestingLimitingJsonWriter` stops at `DeepestNesting`, 1,000 levels, or where the stack runs
  out, so a deep graph can't take the test process down. Nothing limits the size. `MustSerialization.LongestTextShownInFull`,
  100,000 characters, only decides what the message shows: the whole text is built first, and what's longer goes to a
  file (`LongTextFiles`). So a wide graph, such as a view model's, can use all the memory.

## The change

Stop serializing at a size limit, as it stops at the depth limit now, and say in the message that it stopped. That
covers every assertion, and `ReferenceEqual`'s messages keep showing the values' contents, which help when two
equal-looking instances turn out to be different ones.

Where to set the limit, and how the message says it stopped, are ours to decide. A starting point: the writer counts
what it has written and throws, as it does for the depth, once the count passes a limit well above
`LongestTextShownInFull`, so that a text written to a file can still be long.

## Once it's released

Tell tooling-with-magnus, so that the tooling can take the new Compze.Must.
