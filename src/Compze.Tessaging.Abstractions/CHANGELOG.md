# Changelog

All notable changes to Compze.Tessaging.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **The package exists: the tessage type system leaves `Compze.Abstractions` and gets its own home.** The `ITessage` hierarchy, the delivery-guarantee and transactional markers, the `TessageTypes` base-class families, `TessageId`, the publisher-identifying tevent wrapper, the publisher/sender door contracts, and the `Validation/` inspectors all moved here from `Compze.Abstractions.Tessaging.Public` and `.Validation`, into the namespaces `Compze.Tessaging.Public` and `Compze.Tessaging.Validation`. What earns the split is that declaring tessages must not mean depending on the engine that delivers them: an application's shared contracts assembly needs the type system alone, and `Compze.Teventive` — which raises tevents with no endpoint anywhere — is the same case inside the framework.
- **`PublisherTevent` and `PublisherTevent<TTevent>` now live in the assembly whose namespace they claim.** They declared `namespace Compze.Teventive.Tevents.Public` while shipping in `Compze.Abstractions`, and were the only declarers of that namespace — a name that pointed at neither the assembly holding them nor the concept routing them. They are `Compze.Tessaging.Public` now: every tevent is wrapped before routing, which makes the wrapper a tessaging routing concept.
- **`PublisherIdentifyingTeventCE` is `PublisherTeventCE`.** The concept was renamed `IPublisherTevent`/`PublisherTevent` earlier; the extension class kept the retired synonym.
