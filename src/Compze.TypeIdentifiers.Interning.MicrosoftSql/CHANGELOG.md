# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `MsSqlEndpointPersistence` is renamed `MsSqlEndpointDatabase` — it declares the endpoint's database (connection pool + type-id interner), not any feature's persistence — and gains the composition form on `ComposeEndpoint`'s foundation, returning `EndpointFoundation<MsSqlEndpointDatabase>`.

## 0.1.0-alpha

- Initial pre-release
