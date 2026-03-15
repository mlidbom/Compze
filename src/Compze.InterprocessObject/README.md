# Compze.InterprocessObject

Create an object shared between processes in a single line of code. Fully atomic modifications, await conditions becoming true, automatic corruption recovery, pluggable serialization.

## Features

- **`IInterprocessObject<T>`** — Read, update, and condition-wait on shared state across processes
- **Atomic operations** — Cross-process mutex ensures safe read-modify-write
- **Corruption recovery** — Automatically recovers from partially written state
- **Pluggable serialization** — Bring your own serializer, or use the MemoryPack plugin

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.InterprocessObject.MemoryPack](https://www.nuget.org/packages/Compze.InterprocessObject.MemoryPack) | MemoryPack serializer support |

## License

Apache-2.0
