# Changelog

All notable changes to Compze.Internals.SystemCE will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- Added `SetInCopy` to the read-only-dictionary copy helpers: like `AddToCopy` but overwrites when the key is already present.
- Fixed `Constructor.GenericTypeConstructor`: its compiled-constructor cache was shared across all instances and keyed only by the argument type, so closing two different generic type definitions over the same argument type handed the second caller the first one's constructor. Concretely: a `DogTaggregate` inheriting `AnimalTaggregate` published its tevents wrapped in the `CatTevent<>` wrapper if a cat had published first.

## 0.2.1-internal

- Refactoring.

## 0.2.1-alpha

- Refactoring.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
