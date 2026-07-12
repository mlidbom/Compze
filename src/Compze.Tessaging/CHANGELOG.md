# Changelog

All notable changes to Compze.Tessaging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- The in-process bus routes exclusively by wrapper type: `ForTevent<TTevent>` keys an inner tevent type subscription under `IPublisherIdentifyingTevent<TTevent>` and unwraps at delivery, while a subscription to a wrapper type receives the wrapper itself - publisher-conscious subscription (subscribe to `IManagerTevent<IEmployeeTevent>` and receive only the employee tevents a manager published). Subscribing to a tevent type and subscribing to its wrapper receive exactly the same tevents.
- `IInProcessTeventPublisher.Publish` wraps a tevent published without a publisher-identifying wrapper before routing, and the inbox wraps tevents received from the wire the same way (the wire still carries inner tevents until the remote-transport increment).
- `ForTevent` now validates the subscribed type with the subscription rules (interfaces only) instead of the general message-type rules, matching the in-memory dispatcher.
- The tevent-store publishers (`InProcessOnlyTeventStoreTeventPublisher`, `DistributedTeventStoreTeventPublisher`) receive the committed tevent in its wrapper and deliver it wrapped in-process; the distributed publisher hands the outbox the inner tevent until the wire carries wrappers.
- Fixed: registering a second tevent handler whose subscription resolves to an already-registered routing key threw instead of appending the handler.

## 0.3.1-alpha

- Fixed packaging: the `_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
