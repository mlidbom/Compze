# Changelog

All notable changes to Compze.Typermedia.Hosting.AspNetCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `TypermediaTransportServer` is gone: `AspNetCoreTypermediaTransportServer()` now contributes the `TypermediaController` to the endpoint's one ASP.NET Core transport server (`Compze.Internals.Transport.AspNet`), registering that server if no other communication style already did.

## 0.1.0-alpha

- Initial pre-release
