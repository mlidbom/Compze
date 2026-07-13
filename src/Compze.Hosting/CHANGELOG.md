# Changelog

All notable changes to Compze.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `InterprocessEndpointRegistry` (`Compze.Hosting.SameMachine`): a same-machine `IEndpointRegistry` and `IEndpointAddressAnnouncer` backed by an `IInterprocessObject` — endpoints announce the address they listen on, every process opening the same registry name and directory sees them, with no configuration and no server. Each entry records its `AnnouncingProcess` (process id + start time, because the OS recycles ids), so a crashed process's stale addresses are never routed to and are pruned on the next announcement.

## 0.1.1-alpha

- Fixed packaging: the `_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.1.0-alpha

- Initial pre-release
