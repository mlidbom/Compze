# Changelog

All notable changes to Compze.Internals.Transport.AspNet will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `AspNetCoreEndpointTransportServer`: the one Kestrel server per endpoint, hosting every communication style's contributed controllers (`AspNetCoreControllerContribution` component set) plus the `InfrastructureQueryController` — consolidating the two near-identical per-style servers that previously lived in `Compze.Tessaging.Hosting.AspNetCore` and `Compze.Typermedia.Hosting.AspNetCore`.

## 0.1.0-alpha

- Initial pre-release
