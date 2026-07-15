# Changelog

All notable changes to Compze.Internals.Sql.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- `MySqlEndpointDatabase`: the declaration that an endpoint's database is MySQL, carried by `EndpointFoundation<MySqlEndpointDatabase>` so the features added on the foundation bind their MySQL sql layers through the compiler. The declaration itself lives here too — `MySqlEndpointDatabase(connectionStringName)` and its `ComposeEndpoint` composition form register the endpoint's connection pool; the sql-layer features wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
