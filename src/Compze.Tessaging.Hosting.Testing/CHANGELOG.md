# Changelog

All notable changes to Compze.Tessaging.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- `ExactlyOnceTessagingTestingEndpointHostFeature` has every endpoint `ParticipateIn` the host's real interprocess endpoint registry (`ITestingEndpointHost.EndpointRegistry`) instead of discovering through a test-only registry listing the host's addresses — every test now runs the production announce/discover pipeline.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
