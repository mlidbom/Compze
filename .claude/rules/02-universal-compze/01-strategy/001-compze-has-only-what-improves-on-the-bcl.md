# Compze has only what improves on the BCL

A Compze type or member is worth having only where it improves on what .NET's base class library offers. Magnus:
"Compze needs to improve on something available in the BCL for a Compze version to be worth it." A better, more
intuitive API is improvement enough: `StartsWithOrdinal` and the other `*Ordinal` string methods in
Compze.Internals.SystemCE do nothing the BCL can't, and read better than passing `StringComparison.Ordinal` each time.
Where the BCL does the job as well, Compze leaves it out, and its code uses the BCL.

- **Before adding to Compze,** look at what the BCL offers, and add only what does better.
- **When Compze has something the BCL does as well,** recommend removing it. The BCL may have gained it since, or it was
  written without knowing that the BCL had it.
- **A BCL type that does the job fully is no gap in Compze:** using it calls for no Compze counterpart. `SemaphoreSlim`,
  for a limit on how many requests are in flight, is an example.
