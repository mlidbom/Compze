# Changelog

All notable changes to Compze.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- Added the `Tevents()` projection: a sequence of `IPublisherIdentifyingTevent<TTevent>` wrappers projects to the wrapped inner tevents.
- The publisher-identifying wrapper interfaces (`IPublisherIdentifyingTevent<>`, `IRemotablePublisherIdentifyingTevent<>`, `IExactlyOncePublisherIdentifyingTevent<>`) have type-identifier mappings, so closed generics over them can travel in persisted and transmitted type references.

## 0.1.0-alpha

- Initial pre-release
