# Compze.Threading.Interprocess.MemoryPack

MemoryPack serialization for the `IInterprocessObject<T>` of Compze.Threading.Interprocess: an object shared between processes,
kept in MemoryPack's fast and compact binary format, created in a single line of code.

```csharp
using var visits = MemoryPackInterprocessObject.NewGlobal("visits", () => new Visits(), CorruptionAction.ReplaceContentWithDefaultAndThrow,
                                                          maxBytes: 4 * 1024, directory);
```

Was Compze.InterprocessObject.MemoryPack.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Threading.Interprocess](https://www.nuget.org/packages/Compze.Threading.Interprocess) | `IInterprocessObject<T>`, and the rest of the synchronization between processes |

## License

Apache-2.0
