# Changelog

All notable changes to Compze.DependencyInjection will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

### Added

- Support for TrackedTransient and Transient lifestyles
	- Transient (formerly UntrackedTransient) matching the behavior of transients in SimpleInjector
	- TrackedTransient matching the behavior of transients in Microsoft.Extensions.DependencyInjection
- Captive dependency validation: rejects Singleton→Transient and Scoped→Transient by default
	- Opt-in via `.AllowSingletonDependent()` and `.AllowScopedDependent()` on transient registrations

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
