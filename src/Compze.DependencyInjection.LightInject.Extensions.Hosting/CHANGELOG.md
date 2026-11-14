# Changelog

All notable changes to Compze.DependencyInjection.LightInject.Extensions.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection.Extensions.Hosting 0.5.0-alpha (was 0.4.0-alpha), Compze.DependencyInjection.LightInject 0.6.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- Version bumped in lockstep with the Compze.DependencyInjection package family (0.4.0-alpha). No functional changes.

## 0.3.0-alpha

- Initial pre-release
