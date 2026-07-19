# Changelog

All notable changes to Compze.Typermedia.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

> **The `Compze.Typermedia.Hosting` package folded into `Compze.Tessaging` (the Typermedia sibling of the Tessaging
> paradigm) on 2026-07-17 - see the paradigm project's changelog from there on. The package's own history is
> preserved below.**


## 0.2.0-alpha

- Removed `ITypermediaTransportServer`: serving is done by the endpoint's one transport server (`IEndpointTransportServer` in `Compze.Internals.Transport`), to which Typermedia contributes its request handling.

## 0.1.0-alpha

- Initial pre-release
