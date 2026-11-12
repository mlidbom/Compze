# Changelog

All notable changes to Compze.Contracts will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.7.2

- When an `Assert` given a message factory fails, the exception's message is the factory's message as written. It was wrapped as if it were a code expression, such as `Argument.Assert(The tokenizer gives no part of speech that is …)`.

## 0.7.1

- `TAssertions.True`'s documentation described a null check. It now describes the assertion it documents: throws when the value is false, returns the value on success.

## 0.7.0

- Internal refactoring; updated for the restructured Compze package layout.

## 0.6.0

### Changed

Major refactorings preparing for 1.0.0:
