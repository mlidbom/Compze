# Remote typermedia calls: deadlines, cancellation and stopping: todo (found by reading, nothing designed)

## Status

Found 2026-11-20 by reading the code, answering the Claude tooling's questions about Cloyd. Nothing is designed or
decided. Magnus: no significant work on Tessaging for Cloyd begins now; this records the gaps.

## The gaps

- **No deadline per call.** `IRemoteTypermediaNavigator`'s `PostAsync`, `Post`, `GetAsync` and `Get` take no
  `CancellationToken` and no timeout. `HandlerAvailabilityPatience` is one value per endpoint and bounds only the wait
  for a route. The named-pipe client has fixed timeouts (`NamedPipeClientConnectionPool`: 10 seconds to get a free
  connection, 10 seconds to connect) and none for the response.
- **No cancellation,** in the process or across the wire. A caller can't give up on a call, and nothing tells the
  receiver to stop: its handler runs to completion. An application has to send a cancel tommand of its own.
- **A request holds a pooled connection for its handler's whole run.** The pool allows 8 exchanges at once per peer
  (`MaximumConcurrentExchanges`). Long-running requests use them up, and the next caller waits 10 seconds for one, then
  fails.
- **Stopping gives the caller no "not done" answer.** `NamedPipeTransportServer.StopAsync` disposes every open stream,
  those whose handler is still running included. The handler isn't cancelled, and its response is lost. A caller sees
  one of four outcomes: success; `TessageDispatchingFailedException` with the remote exception; the same exception
  for a request that was never sent; a raw `IOException`, meaning the caller can't know whether it ran. The two
  dispatch failures are told apart only by their message text. When one process hands its endpoint over to a
  successor, nothing says "stopping, not executed, send it again".
- **No deduplication, and the receiver retries.** Nothing deduplicates a typermedia tommand by its `TessageId`, and
  `TypermediaHandlerExecutor` runs a handler that threw up to 5 times: see the `review:` comment there.

## Options that came up (none chosen)

- A `CancellationToken`, or a deadline, on each navigator call, carried over the wire so the receiver can cancel the
  handler.
- At stop: answer arriving requests with a typed "stopping, not executed", drain those already running, then close.
- Typed exceptions for "never sent", "failed remotely" and "unknown whether it ran".

## Related

- [Addressing one endpoint among several](addressing-one-endpoint-among-several.md).
- [Native AOT](native-aot.md).
