# Changelog

All notable changes to Compze.Internals.Sql.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- `MsSqlDomainDatabase(connectionStringName)`: declares the domain database this endpoint joins — registers the connection pool every sql layer the endpoint registers stores its data through; the sql layers wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
