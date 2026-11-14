# Changelog

All notable changes to Compze.DependencyInjection.DryIoc.Extensions.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- Version bumped in lockstep with the Compze.DependencyInjection package family (0.4.0-alpha). No functional changes.

## 0.3.0-alpha

- Initial pre-release
