# The slot as a static of its owner's class, on the combined prototype (the Fable session)

An experiment, not for merging, on the branch `taggregate-experiments-fable-static-slots`, from `taggregates-opus/combined`.
The conclusions are in `dev_docs/taggregate-improvements/shared` on the branch `taggregate-improvements`.

- **In Compze.Teventive:** `SharedTomponentSlotOfAClass`, a slot that belongs to its owner's class and is kept in a static
  field; an entry for it in `TeventRouting<TInstance>.Declaration`; and a constructor of `SharedTomponent` that takes the
  slot and the owner.
- **Specified** in `test/Compze.Tests.Unit/CQRS/Taggregates/SharedTomponentSlotsOfAClass`: a slot in the taggregate itself,
  two slots of one class in a component, loading from history and from a snapshot whose adopted tevents the slots made,
  what a subscriber can subscribe to, and a slot declared below the routing that enters it.
- **Measured** by `SlotsOfAClassMeasured`, against this worktree's Compze.Teventive:

  ```
  dotnet build src/Compze.Teventive
  cd experiments/taggregate-improvements-fable/SlotsOfAClassMeasured
  dotnet run -c Release
  ```

The build files in this folder stop the repository's own from applying to the measuring program.
