# Changelog

All notable changes to Compze.Teventive will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.2-alpha

- Fixed packaging: the `Taggregates\_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `Taggregates\_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.3.1-alpha

- Dispatcher configuration is now immutable and supplied at creation: `IMutableTeventDispatcher.New` takes a `TeventDispatcherConfig` (option flags plus tevent types ignored when unhandled), replacing the mutating `IgnoreUnhandled`/`IgnoreAllUnhandled` registration methods.
- Renamed `ITeventHandlerRegistrar` to `ITeventSubscriber`: the object through which a party subscribes handlers to a dispatcher's tevents.
- `ITeventSubscriber` is now `IDisposable`: disposing a subscriber removes every subscription made through it from the dispatcher. Disposing twice is harmless; registering through a disposed subscriber throws.

## 0.3.0-alpha

- Initial release of extracted project
