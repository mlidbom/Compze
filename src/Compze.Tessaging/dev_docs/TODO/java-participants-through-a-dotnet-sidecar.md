# Java participants through a .NET sidecar: todo (designed in discussion, nothing built)

## Status

On 2026-11-20 Magnus asked whether a minimal Tessaging implementation in Java could take part in the tessaging on a
machine. Four sessions took part:

- java-tessaging, here;
- compze-rep, for Compze;
- tessaging-in-tooling, for the Claude tooling in `~/.claude`;
- tessinging-in-cloyd, for Cloyd, the tooling's plugin for JetBrains IDEs.

What they found came from reading the code, plus one measurement. Magnus: "this is all design and considering so far.
Do not start doing any significant porting effort." The work paused the same day, and nothing is built.

- **Decided (Magnus, 2026-11-20):** a .NET sidecar per IDE, not a Java port of Tessaging. The link between the IDE and
  its sidecar is ours to choose: "With no tessaging in java we can use whatever protocol we like best."
- **Open:** listed under "Open decisions".

## Why

Several JetBrains IDEs run on the machine at once, each with Cloyd loaded. Cloyd talks to the Claude tooling's hub over
HTTP today, and each kind of tooling process has an announcement file and an HTTP API of its own. On Tessaging, all of
them would find and address each other one way. For Compze, this would be the wire's first long-running, everyday use.
Deskmancer's black-box specifications already use it, one worker with one observer per run. The tooling would add
several instances of one role, processes replaced under the same id, and long calls.

## Why not a Java port

What a Java participant would have to reproduce, none of it versioned or documented outside the code:

- **The framing** (`NamedPipeFraming`):
  - A request is a kind byte, then the `TessageId`, the payload's type id string and the body. Each string is a
    little-endian int32 UTF-8 byte length, then the bytes.
  - Kinds 1 and 2 also carry a `DeliveryStreamPosition`.
  - A response starts with a status byte: 1 is success, followed by the payload; 2 is an error, followed by the
    exception's type and detail.
  - Request kinds: 1 exactly-once tevent, 2 exactly-once tommand, 3 typermedia tuery, 4 typermedia tommand with a
    result, 5 typermedia void tommand, 6 endpoint discovery, 7 best-effort tevent.
  - Addresses are `compze.pipe://localhost/Compze.<guid>`, and pipes are created `CurrentUserOnly`.
- **Discovery:** request kind 6 is answered with `EndpointInformation` `{Name, Id, HandledTessageTypes}`, serialized
  by System.Text.Json.
- **Type identity:**
  - Types are named by TypeIdentifier's canonical strings, with ECMA-335 brackets for generics (see
    `src/Compze.TypeIdentifiers/dev_docs/TypeIdentifier-Standard-Draft.md`).
  - Every tevent travels wrapped in `PublisherTevent<T>`, and subscriptions are advertised as `IPublisherTevent<X>`.
  - A sender picks a tevent's receivers by .NET assignability. A Java side would have to reproduce the assignability
    of the whole tevent hierarchy.
- **Bodies:** Newtonsoft, with `TypeNameHandling.Auto`, PascalCase, private setters and indented formatting. `$type`
  values are rewritten to canonical type ids.
- **The registry** (`InterprocessEndpointRegistry`):
  - The memory-mapped file holds a 4-byte length header, then a `BinaryWriter` payload: a count, and per entry the
    Guid's bytes, a string with `BinaryWriter`'s 7-bit length prefix, an int32 process id and int64 UTC start ticks.
    The file holds at most 64 KB.
  - It is locked by a Win32 named mutex, `Local\<name>`.
  - Readers poll a change counter in a `.signal` file.
  - A process counts as alive by its id and its start time, within 1 second.

On the Java side, none of this is impossible:
- Erasure is manageable: the wire carries the closed type, and declared generic interfaces keep `TResult`.
- JNA is bundled with the IDE (`com.intellij.modules.jna`), for the mutex and the mapped file.
- Plugins compile to Java 21, where the foreign function API is still a preview.
- The IDE's runtime, JBR, is 25.0.4.1.

But a port would follow every change to Compze's wire by reading Compze's code, for as long as it existed.

## The bridge options, ranked

1. **A .NET sidecar per IDE.** Chosen.
2. **CoreCLR hosted in the IDE's process through hostfxr,** as JAStudio hosts .NET from Python with pythonnet. Ruled
   out: Cloyd is reloaded dynamically several times a day, and a .NET runtime can't be unloaded from a process.
3. **Rider's own .NET backend.** Rejected: there is one per solution, and Rider only.
4. **The hub,** as today: Cloyd stays on HTTP, and the hub speaks Tessaging for it.
5. **A Java port.** Last, for the reasons above.

## The sidecar, as designed so far

- **One per IDE process,** started by Cloyd: framework-dependent on .NET 11, linked to the IDE over its stdin and
  stdout.
- **It ends on any of three signals:**
  - It waits on the IDE's process handle, which it opens at its own start.
  - At an unload, in `DynamicPluginListener.beforePluginUnload`, which runs on the EDT, Cloyd sends an explicit stop
    message over stdin, waits a few hundred milliseconds, then destroys the process.
  - End of file on stdin is a third signal, relied on for nothing.
- **It stays inside the IDE's job object.**
- **A plugin reload restarts it,** which costs about a second of absence from Tessaging.
- **Measured:** `test/Compze.Tests.SameMachine.EndpointHostProcess` stood in for it. Over five runs, it took 131-181 ms
  from process start to announced and sending, with about 40 MB working set and 14 MB private. Caveats: a Debug build,
  alone in its registry.
- **Next to explore:** the test that the sidecar exits when only the IDE's main process is killed.
- **Its `EndpointId`:** a stable one per IDE instance, or a fresh one per start, is the question
  [addressing one endpoint among several](addressing-one-endpoint-among-several.md) raises.

## Recommended scope (open)

java-tessaging and tessaging-in-tooling agree on the first part. The revised conversation streams below are
java-tessaging's alone.

- **First:**
  - discovery and addressing, replacing the announcement files;
  - the request/response calls: Cloyd to the hub, the hub to one IDE, Cloyd to Cloyd, Deskmancer to an IDE. Typing
    stays one request of up to 2 minutes;
  - the running-sessions list, as a tevent plus a snapshot tuery.
- **Later:** the conversation streams, until then on Server-Sent Events.
- **Before any of it,** the gaps below that the calls need closed.

## The conversation streams: the volume, measured

Each Cloyd tab follows one conversation from its start. Routing by type narrows by kind of tevent, never by
conversation. A tab following session S wants every kind from S and nothing from the others, so every Cloyd gets every
conversation's tevents.

That doesn't matter at this machine's rates. Counted over every Claude transcript, subagents included, for the week to
2026-11-20:
- busiest minute: 240 lines, about 4 a second, across 8 sessions;
- heaviest minute: 4.3 MB;
- busiest day: 40,744 lines;
- line size: median 2 KB, largest 1.3 MB.

`Remote_Tuery_Performance_tests` allows at most 100 ms for 100 single-threaded remote tueries, and 20 ms for 100 async
ones. That is at least 1,000 to 5,000 a second, not measured here.

So the recommended design is to push the appends themselves:
- Each append carries its conversation and its position in it.
- Each sidecar discards the conversations it doesn't follow, before anything reaches Java.
- A paged tuery fills a newly opened tab, or a gap such as the sidecar's absence at a reload.

The earlier design, notify-then-pull, sent a small tevent per append and had the following Cloyds pull. It needed the
same tuery, plus a round trip per append.

**The precondition is the count, for either design.** Each reload leaves a departed peer. The publisher's queue for it
fails publishes at 10,000 tevents (`BestEffortTeventQueues.MaximumQueuedTeventsPerPeer`): after about 40 minutes at the
busiest, about 5 hours on a busy day. See the ephemeral subscriber under "Open decisions".

## Gaps in Compze found on the way

compze-rep found most of these by reading:

- **No way to address one endpoint among several:** [its own document](addressing-one-endpoint-among-several.md).
  Magnus: it "probably needs to change no matter what we decide here".
- **No per-call deadline.** Patience bounds only the wait for a route; the wait for the response has no timeout.
- **No cancellation,** locally or across the wire.
- **A request holds one of the 8 pooled connections to its peer for its handler's whole run,** so long requests block
  the peer. tessaging-in-tooling: fix it in Compze by multiplexing, not by reshaping the callers around it.
- **A remote typermedia tommand whose handler throws is retried up to 5 times,** and nothing deduplicates by
  `TessageId`: a `review:` comment in `TypermediaHandlerExecutor`, commit 00d42ee93. This is the most dangerous gap for
  the tooling, whose handlers nearly all act outside any transaction.
- **No "not done" outcome** when the hub hands over to its successor.
- **Retracting an endpoint removes a successor's announcement under the same id:** a `review:` comment on
  `InterprocessEndpointRegistry.RetractEndpointAddress`, commit 1913b8935.
- **Nothing forgets a peer,** so a departed subscriber's best-effort queue fills (above). `DoNotQueueTeventsFor` needs
  the peers' `EndpointId`s at the publisher's composition.
- **No Native AOT.**
- **The registry's mutex is named by the registry name alone,** not its directory.

## Open decisions

Magnus's, put to him on 2026-11-20 and deferred with the work:

- **The scope above:** a partial join, with discovery and calls first, conversation streams later and pushed.
- **A protocol version in the discovery answer, and a document of the wire protocol.** The sidecar ships with Cloyd,
  and the tooling's processes are built on their own, so their Compze versions can differ. A mismatch fails obscurely
  today, and the wire is defined only by the code.
- **An ephemeral subscriber:** a subscriber declares in its advertisement that publishers neither queue for it while
  it is absent nor remember it once it has gone. A sidecar that restarts with each reload needs it.

## Related

- [Same-machine hosting](../../../Compze.Hosting/dev_docs/wip/same-machine-hosting.md)
- [Peers](../peers.md)
- [A TCP transport and discovery across machines](tcp-transport-and-discovery-across-machines.md), on the branch
  `tcp-protocol`.
- **The tooling side,** outside this repository: `~/.claude/dev_docs/tessaging-in-the-tooling.md`, in
  mlidbom/home-claude (commit 241f181). It covers why the tooling would join, its processes and their calls, and the hub
  handover.
- **Cloyd's requirements,** outside this repository: `dev_docs/claude-sessions.md` in ClaudeSymbolLinker (commit
  130b271), section "Talking to the hub through Tessaging (considered, not built)".
