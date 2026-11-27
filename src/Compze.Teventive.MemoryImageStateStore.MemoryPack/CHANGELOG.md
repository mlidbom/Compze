# Changelog

All notable changes to Compze.Teventive.MemoryImageStateStore.MemoryPack will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.1-alpha

- **Requires exact versions of the Compze packages whose internals it uses:** Compze.Teventive.MemoryImageStateStore 0.4.2-alpha (was 0.1.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.1.0-alpha

- **A MemoryPack cache for a memory image state store kept in JSON files,** declared with `files.StartingFromAMemoryPackCache().FolderOf<TTaggregate>(folder)`, so the store starts without reading every file.
  - A cache file for each class, in the store's own folder, holds each file's path, length and last write time with the snapshot it held. At the next start, a file whose length and last write time are unchanged takes its snapshot from the cache. A new or changed file is read, and a gone one is dropped.
  - The cache is written after a start that read any file or found one gone, whole, under a temporary name and renamed. Commits don't write it: the files a commit wrote are read at the next start.
  - A cache written for another shape of a class's snapshot type isn't used: it records a fingerprint of the type's instance fields, and of the types reachable through them, and the start reads every file of the class and writes the cache anew. MemoryPack reads by position, so such a cache could otherwise read back wrong values without failing.
  - A cache that can't be read fails the start, naming it. Deleting it gives a start that reads every file.
  - Each class's snapshot type must be one MemoryPack serializes, such as a `[MemoryPackable]` class: asserted where the store is composed.
