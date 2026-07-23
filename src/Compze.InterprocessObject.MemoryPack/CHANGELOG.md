# Changelog

All notable changes to Compze.InterprocessObject.MemoryPack will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.1-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-alpha

- Initial pre-release. [MemoryPack](https://github.com/Cysharp/MemoryPack)-based implementation of `IInterprocessObjectSerializer<T>` for use with Compze.InterprocessObject.
