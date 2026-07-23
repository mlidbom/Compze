# Changelog

All notable changes to Compze.TypeIdentifiers.Interning will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.1-alpha

- Members that only the interner's own machinery reads leave the public surface: `TypeIdInterner`'s constructor (it is resolved, never constructed by hand) and `InternerSnapshot`'s `Types` and `Spellings` projections.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.1.0-alpha

- Initial pre-release
