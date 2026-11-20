# Addressing one endpoint among several: todo (wanted, nothing designed or built)

## Status

Magnus, 2026-11-20, on the gap: it "probably needs to change no matter what we decide here". He wrote it while
answering whether the Claude tooling's Cloyd should communicate over Tessaging, so it's wanted whatever that answer is.
Nothing is designed, decided or started. This document records the gap and the options that came up, found by reading
the code.

## Why

Several endpoints can serve the same role on one machine, such as one per running IDE instance for the Claude
tooling's Cloyd. A sender has to reach one particular instance: the IDE a given Claude session runs in. Deskmancer's
black-box specifications already run over the named-pipe transport, and the tcp-protocol branch was built for them. The
tooling would be the first long-running, everyday use with several instances of one role.

## What exists today

- **A request/response type needs exactly one live route.** `HandlerAvailability.AwaitAddressOfTypermediaHandlerForAsync`
  waits, for up to the endpoint's patience (`HandlerAvailabilityPatience`, 30 seconds by default), until
  `ITessagingRouter.TypermediaRoutesFor(tessageType)` lists exactly one route. With two or more it throws
  `MultipleHandlersForTypermediaTypeException` once the patience runs out. Routing is by the exact type
  (`TessagingRouter._typermediaHandlerRoutes`).
- **An exactly-once tommand binds to one receiver,** the live handler or the sole remembered one
  (`AwaitBindableReceiverOfAsync`). With several remembered handlers it throws `MultipleHandlersForTessageTypeException`.
- **Discovery tells a sender only who an endpoint is and what it serves:** `EndpointInformation` is
  `{Name, Id, HandledTessageTypes}`. It carries nothing that tells two instances of one role apart, such as a process
  or a workspace.
- **The `EndpointId` is the application's choice** (`EndpointConfiguration`), and peers are remembered by it forever.
  An application that creates a fresh id at each start leaves a remembered peer behind at each restart.
- **`IRemoteTypermediaNavigator`** (`PostAsync`, `Post`, `GetAsync`, `Get`) takes no endpoint to send to.
- **A connect to a pipe that is gone** waits out the named-pipe client's 10-second connect timeout.

## What can be done today, without changing Compze

Give each instance its own tessage types, or put the target's identity in the tessage, such as the process id and
start time, and have every other instance's handler refuse it. Neither is good: the first needs as many types as
instances, and the second still fails on exactly-one-route, since every instance advertises the type.

## Options that came up (none chosen)

- **Advertised properties plus addressing by `EndpointId`:** an endpoint advertises properties of its own besides its
  tessage types, such as the workspace it serves, and a sender picks an endpoint by them, then sends to it, as in
  `navigator.At(endpointId).GetAsync(...)`. The exactly-one-route rule then applies only to a send that names no
  endpoint.
- **A stable `EndpointId` per instance,** such as one derived from the IDE's configuration directory, so that a
  restarted instance is the same peer and not a new one that is remembered forever.

## Related

- [Peers](../peers.md): how peers are remembered, and `RequirePeers` and `DoNotQueueTeventsFor`.
- [A TCP transport and discovery across machines](tcp-transport-and-discovery-across-machines.md): the registry it
  designs has the same question to answer.
