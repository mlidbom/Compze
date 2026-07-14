# Changelog

All notable changes to Compze.Tessaging.Hosting.AspNetCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `AspNetInboxTransportServer` is gone: `AspNetCoreTessagingTransport()` now contributes the `TessagingController` to the endpoint's one ASP.NET Core transport server (`Compze.Internals.Transport.AspNet`), registering that server if no other communication style already did.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
