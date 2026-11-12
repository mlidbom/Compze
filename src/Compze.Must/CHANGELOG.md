# Changelog

All notable changes to Compze.Must will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.2-alpha

- **A failure message never fails.** When Must cannot serialize a value, the `AssertionFailedException` still carries the assertion's own message, and in place of the value's JSON it says what was thrown, while reading which member, at which path, and the configuration line that leaves the member out. Before, the serializer's exception replaced the assertion's message.
- **Nothing Must serializes can kill the test process any more.** Each of these did, taking every other test in the run with it:
  - A graph nested deeper than the thread's stack allows: Must now stops before the stack overflows and reports it, about a thousand levels per megabyte of stack. The `MaxDepth = 32` setting meant to prevent this never applied to writing JSON, and is gone.
  - A getter throwing a few hundred levels down: Newtonsoft rethrew the exception at every level it unwound through, which overflowed the stack. Every error is now handled where it is thrown.
  - A delegate or a reflection object anywhere in the graph: serializing its members called the runtime's private getters, and one killed the process with a fatal CLR error (`0x80131506`). Delegates, methods, properties, fields, events, constructors, parameters, assemblies and modules are now written as a short description.
- A task is written as its status, its result once it has one, and its exception's message if it faulted. Reading the result of a task that never completed hung the test forever.
- Members nothing can read are left out instead of failing the serialization: indexers, properties without a getter, and pointer, by-ref and ref-like members such as `Span<T>`. Value tuples, `JsonElement` and `JsonNode` serialize as a result.
- A dictionary that implements only the generic `IDictionary<TKey, TValue>`, such as `JsonObject`, serializes; it threw an `InvalidCastException`.
- **Serialization rules for one assertion chain** go on the `Must` call, `actual.Must(serialization: it => ...)`, through `ISerializationConfig<TValue>`: `ExcludeMember` (by expression, or by declaring type and name for private members and unnameable types), `ExcludeType`, `SerializeAs` and `WithoutTypeNames`. They apply to the chain's failure messages and to `DeepEqual`.
- **Serialization rules for every assertion** go on `MustSerialization`, set once as the test assembly starts: `ExcludeType`, `ExcludeMember` and `SerializeAs`. A chain's own rules take precedence.
- **Breaking:** `DeepEqual`'s `config` parameter and `DeepEqualConfig` are gone; their rules moved to the `Must` call. `config => config.ExcludeMember(...)` becomes `.Must(serialization: it => it.ExcludeMember(...))`, and `IgnoreTypes()` becomes `WithoutTypeNames()`.
- Text too long for a failure message, longer than `MustSerialization.LongestTextShownInFull` (100,000 characters by default), is written to a file under the test assembly's output folder, `MustOutput`, and the message shows its start and the file's path.

## 0.4.1-alpha

- The assertion machinery moves below `_private` — the assertion context, the call-name reader, the diff generator and the invoking-must-throw extensions. The `Must` vocabulary itself is unchanged.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

- Move extension methods back to `Compze.Must` namespace.

## 0.3.0-alpha

- The assertion classes moved from the `Compze.Must` namespace to `Compze.Must.Assertions` — the namespace now matches the folder they always lived in. Consumers add `using Compze.Must.Assertions;` beside `using Compze.Must;`.

## 0.2.1-alpha

- First release under the new name. Previously published as [Compze.Utilities.Testing.Must](https://www.nuget.org/packages/Compze.Utilities.Testing.Must/).

## 0.2.0-alpha.1 *(as Compze.Utilities.Testing.Must)*

Refactoring.

## 0.1.0-alpha.3 *(as Compze.Utilities.Testing.Must)*

- Initial pre-release
