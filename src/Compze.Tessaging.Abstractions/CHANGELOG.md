# Changelog

All notable changes to Compze.Tessaging.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `ITransactionIgnoringTeventHandlerRegistrar` — registers transaction-ignoring tevent handlers: observation, the one subscription-side opt-down from a tevent type's declared delivery guarantee (see `src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`). A separate registrar from `ITessageHandlerRegistrar`, deliberately: opting out of every delivery guarantee is visible and off the common surface. `TransactionIgnoringTeventHandlerRegistrarCE` gives it the same resolved-dependency convenience overloads the ordinary registrar has.
- `TessageHandlerRegistrarWithDependencyInjectionSupport` is removed: it added literally nothing to a raw `ITessageHandlerRegistrar`. The resolved-dependency `ForTevent`/`ForTommand` convenience overloads now extend `ITessageHandlerRegistrar` itself (`TessageHandlerRegistrarCE`), and every surface that handed out the wrapper hands out the interface.

## 0.2.0-alpha

- Initial pre-release
