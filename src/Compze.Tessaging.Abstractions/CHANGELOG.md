# Changelog

All notable changes to Compze.Tessaging.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

- **The namespaces drop the `.Abstractions` segment: the type system is `Compze.Tessaging` itself.** The
  `ITessage` hierarchy, `TessageId` and the base-class families now declare `namespace Compze.Tessaging`; the
  send/publish contracts are `Compze.Tessaging.TessageBus`; the inspectors are `Compze.Tessaging.Validation`.
  Declaring a tessage is the most common consumer act in the framework, and it deserves the paradigm's root
  namespace — the package name stays `Compze.Tessaging.Abstractions` only while the project topology settles.
- **The package exists: the tessage type system leaves `Compze.Abstractions` and gets its own home.** The `ITessage` hierarchy, the delivery-guarantee and transactional markers, the `TessageTypes` base-class families, `TessageId`, the publisher-identifying tevent wrapper, the publisher/sender contracts, and the `Validation/` inspectors all moved here from `Compze.Abstractions.Tessaging.Public` and `.Validation`, into the namespaces `Compze.Tessaging.Public` and `Compze.Tessaging.Validation`. What earns the split is that declaring tessages must not mean depending on the engine that delivers them: an application's shared contracts assembly needs the type system alone, and `Compze.Teventive` — which raises tevents with no endpoint anywhere — is the same case inside the framework.
- **`PublisherTevent` and `PublisherTevent<TTevent>` now live in the assembly whose namespace they claim.** They declared `namespace Compze.Teventive.Tevents.Public` while shipping in `Compze.Abstractions`, and were the only declarers of that namespace — a name that pointed at neither the assembly holding them nor the concept routing them. They are `Compze.Tessaging.Public` now: every tevent is wrapped before routing, which makes the wrapper a tessaging routing concept.
- **`PublisherIdentifyingTeventCE` is `PublisherTeventCE`.** The concept was renamed `IPublisherTevent`/`PublisherTevent` earlier; the extension class kept the retired synonym.
- **The tessage-type design rules become a first-class testing surface.** `TessageTypeDesignRules` is public — what a consumer runs their own tessage types through — while `TessageTypeInspector` and `TessageValidator` go internal: the rules are the surface, the inspector is machinery.
- **The handler-registration interfaces leave for `Compze.Tessaging`.** `ITessageHandlerRegistrar` and `ITransactionIgnoringTeventHandlerRegistrar` (with their extension classes) are registration surfaces of the engine, not of the type system; each communication style declares its own registrar there now.
- `ITyperMediaTessage<TResult>` is `ITypermediaTessage<TResult>`: one casing for one concept.
- The result-bearing strictly-local tommand kind exists again — `IStrictlyLocalTommand<TResult>` and `StrictlyLocalTommand<TResult>`. Strictly-local tessages never cross a process boundary, so they are exempt from the rule that forbids requiring and forbidding a transactional sender at once.
- `IExactlyOneReceiverTessage`: the marker the single-handler kinds share, and what peer memory answers the single-handler question for.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- `ITransactionIgnoringTeventHandlerRegistrar` — registers transaction-ignoring tevent handlers: observation, the one subscription-side opt-down from a tevent type's declared delivery guarantee (see `src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`). A separate registrar from `ITessageHandlerRegistrar`, deliberately: opting out of every delivery guarantee is visible and off the common surface. `TransactionIgnoringTeventHandlerRegistrarCE` gives it the same resolved-dependency convenience overloads the ordinary registrar has.
- `TessageHandlerRegistrarWithDependencyInjectionSupport` is removed: it added literally nothing to a raw `ITessageHandlerRegistrar`. The resolved-dependency `ForTevent`/`ForTommand` convenience overloads now extend `ITessageHandlerRegistrar` itself (`TessageHandlerRegistrarCE`), and every surface that handed out the wrapper hands out the interface.

## 0.2.0-alpha

- Initial pre-release
