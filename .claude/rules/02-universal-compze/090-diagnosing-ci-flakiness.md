# Diagnosing CI flakiness: read the load, don't assume it

When a CI test flakes — a connect timeout, a slow query, a race that only shows on the runner — "the VM was
overloaded" is the *last* hypothesis, not the first. That reflex has never once held up here; every instance
hid a real code cause (a race, a lock-release ordering bug, a lock inversion) that a fix resolved.

The evidence is already captured — read it before theorising. The CI `Build and Test` step
(`.github/actions/build-and-test/action.yml`) prints a `vmstat` load summary every run, pass or fail:
peak/avg run-queue (`r`), min idle CPU%, iowait, and the busiest samples — judge them against the printed
`logical CPUs`. Separately, `NamedPipeTransportClient` puts the ThreadPool state (free workers, pending
items) into the exception when a connect times out. Saturated looks like run-queue far above the CPU count
with idle near 0, sustained; healthy looks like run-queue near/below it with idle to spare.

Measured baseline (green run): **4 vCPU, idle never below 20%, run-queue ~2 avg / ~6 peak** — a comfortably
loaded box, ~1.5× a dev machine on pure-CPU work. So a 10-second timeout there is something waiting on an
event that isn't arriving, a code or DB cause — not the machine. (`COMPOSABLE_MACHINE_SLOWNESS=5.0` is
conservative padding for perf-test thresholds only, not a measurement of the box.)

A second fingerprint the instruments catch: ThreadPool **pending work draining at ~1 item/second while live
threads climb by ~1/second** is thread-pool starvation from *blocking* code — the machine is fine; the process
is strangling itself, because the pool injects threads that slowly once its minimum is busy. Async code must
never block a pool thread. The 2026-07 case: Windows' named-pipe connect is synchronous under the hood and ran
per-send; fixed by the transport's connection pooling. Reproduce such cases locally with
`DOTNET_PROCESSOR_COUNT=4` — small-machine starvation is invisible on a many-core dev box.
