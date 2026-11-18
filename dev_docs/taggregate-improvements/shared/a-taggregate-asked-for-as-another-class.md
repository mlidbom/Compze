# A taggregate asked for as another class: the tevent store names both classes (end review defect D12)

> Found by the end review's documentation pass, 2026-11-18, by reading `TaggregateTeventStore`; fixed without asking, as
> the review's other defects were (`reviews/end-review-consolidated.md`, section 1).

## The defect

`ITaggregateStoreReader.Get`, `TryGet` and the updater's `GetForUpdate` and `TryGetForUpdate` promise to throw for an id
the store holds as another of its classes, such as a vocab's id asked for as a sentence. The memory image state store
throws with a message naming both classes. The tevent store throws too, but from deep in the replay of the history: an
`InvalidCastException` when the first tevent of the vocab's history is applied to a sentence, naming neither the id nor
either class.

## The fix

Before the tevent store applies a history to a new instance of the class asked for, it checks that the history's tevents
are of that class's tevent type, and throws an `InvalidOperationException` that names:

- the id;
- the class asked for;
- the declared class whose tevents the history holds, or, if the declared classes don't tell it apart, the type of the
  history's first tevent.

The check is on the history the store has already read, so it costs no further read. A history of a class the store
doesn't keep can't arise: a tevent store keeps only its declared classes.

## Specification

Beside the existing specifications of asking for a taggregate as another class, through the public API, for the tevent
store and the memory image state store alike where both promise it:

1. After a unit of work adds a taggregate of one declared class, `Get` of its id as another declared class throws, and the
   message names the id and both classes.
2. The same for `TryGet`, `GetForUpdate` and `TryGetForUpdate`, inside a unit of work.

The memory image state store already passes them; the tevent store passes them after the fix. The full suite passes.
