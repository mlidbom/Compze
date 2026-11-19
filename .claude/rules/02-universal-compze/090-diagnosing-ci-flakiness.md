# Diagnosing CI flakiness: read the load, don't assume it

When a CI test flakes — a connect timeout, a slow query, a race that only shows on the runner — "the VM was
overloaded" is the *last* hypothesis, not the first. That reflex has never once held up here; every instance
hid a real code cause (a race, a lock-release ordering bug, a lock inversion) that a fix resolved.

## When a CI test flakes or a CI run fails: load the skill `diagnosing-ci-flakiness`

It has what each CI run records of the runner's load and the thread pool, what a healthy runner measures, and how to
hunt a flake that won't reproduce locally. Don't rerun CI hoping for green: a failure is a finding.
