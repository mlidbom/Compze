# Changelog

All notable changes to Compze.DependencyInjection.Autofac will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

### Fixed

- The resolver handed to singleton and transient factory methods is now valid for the created component's lifetime, not just the current resolve operation (it wraps the owning `ILifetimeScope` rather than the operation-scoped `IComponentContext`). Required for the deferred `IServiceResolver<TService>` resolution added in [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) 0.4.0-alpha, which stores the resolver and resolves through it after construction.

## 0.3.0-alpha

- Initial pre-release
