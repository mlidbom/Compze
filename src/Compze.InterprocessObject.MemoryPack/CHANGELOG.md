# Changelog

All notable changes to Compze.InterprocessObject.MemoryPack will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.1-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-alpha

- Initial pre-release. [MemoryPack](https://github.com/Cysharp/MemoryPack)-based implementation of `IInterprocessObjectSerializer<T>` for use with Compze.InterprocessObject.
