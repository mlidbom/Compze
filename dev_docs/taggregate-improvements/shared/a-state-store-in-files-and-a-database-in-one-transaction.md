# A state store kept in files and a database in one transaction: refused by default

> Some names and API here were changed by later decisions of the end review (2026-11-18): see
> [the-naming-pass.md](the-naming-pass.md) and [reviews/end-review-consolidated.md](reviews/end-review-consolidated.md).

**Decided by Magnus, 2026-11-18** (the end review's first question): strict by default, with an exception that says why
and how to turn the refusal off. "That will do. Until the mythical day of legend when we implement proper recovery and
can participate in distributed transactions." Specified here before it was built.

**Built, 0ecee1268,** with all seven specifications; the full suite passes, 5175, and the integration CQRS
specifications on SQL Server, MySQL and PostgreSQL. Found while building:
- **Specification 2 as written can't happen through the tevent store:** it writes each tevent as it is published, so its
  connection is always in the transaction before the files join at the prepare. The specification makes a database
  connection join after the files with a participant that reads a history at its own prepare.
- **Any durable participant that shares a transaction with nothing else must commit in a single phase:** .NET escalates
  at once for a durable participant that doesn't implement `ISinglePhaseNotification`, the first one included. That is
  also why the files' participant implements it.
- And `FilesOnDisk.Delete` threw for a file whose folder was gone, though `IFileSystem.Delete` promises nothing happens
  for a missing file; it now keeps the promise.

## The problem

A state store kept in files writes its files as a volatile participant of the unit of work's transaction. A database
connection is a participant too. They aren't atomic across a crash: with SQL Server the database commits first and the
files after, and a process that dies between the two, or stops itself because a file operation failed, loses the files'
change. In an exactly-once endpoint that breaks the delivery guarantee: peers may receive a tevent about a change that
was then lost, and a tessage may count as handled while its change was lost. Found by both reviewers of the end review.

## What a consumer sees

- **A transaction in which a state store writes files and a database connection of Compze takes part is refused,** by
  whichever of the two joins second. Compze's database connections are those of the tevent store, of the exactly-once
  outbox and inbox, and of anything else on Compze's SQL layer. A connection takes part from when it is opened in the
  transaction, read or write, since nothing can know beforehand whether it will write. The exception names both, says
  what a crash between their commits would lose, and names the declaration that turns the refusal off.
- **A unit of work that only reads the state store is never refused:** the files join the transaction only when the unit
  of work has something to write to them.
- **A SQL Server connection opened outside Compze is refused by .NET itself,** with its `NotSupportedException` about
  implicit distributed transactions, since both it and the files are durable participants. A PostgreSQL, MySQL or SQLite
  connection opened outside Compze isn't seen: their drivers enlist as volatile participants. `StoredInFiles`'s
  documentation says so.
- **The files never commit as part of a distributed transaction, whatever is allowed:** they refuse a transaction that is
  already distributed when they join it, and vote to roll back if asked to prepare in two phases.
- **A failure while writing the commit's journal rolls the unit of work back,** as a `TransactionAbortedException` with
  the failure inside, instead of stopping the process. **A failure after the journal is written still stops the
  process,** as today: renaming the temporary files into place comes after the journal, and that is where a file another
  program holds fails. The next start finishes such a commit from its journal.
- **The opt-out, a declaration on the store's files:** `files => files.AllowedInTransactionsThatAlsoWriteADatabase()`,
  the name a placeholder for the naming pass. The files are then a volatile participant, as today, and nothing is
  refused: they commit after SQL Server's commit, and in enlistment order with the other databases; a crash between the
  commits loses the files' change; a failure committing them stops the process. It has to be volatile: a durable
  participant can't share a transaction with SQL Server's connection at all without a distributed transaction.
- A container may hold the state store and the tevent store either way. Only one transaction holding both is refused.

## How it is built

- **The files as a durable participant that commits in a single phase:** an `ISinglePhaseNotification`, enlisted with
  `EnlistDurable` by the state store's session during the unit of work's prepare, which runs in phase 0
  (`TaggregateUnitOfWork` enlists with `EnlistDuringPrepareRequired`), and only when the prepared write writes or deletes
  a file. .NET accepts a durable enlistment in phase 0, and commits it before the volatile participants (an experiment on
  .NET 11: the unit of work's prepare, then the database's prepare, then the files' single-phase commit, then the unit of
  work's commit and the database's).
  - `SinglePhaseCommit`: writes the journal and carries it out. A failure before the journal is in place deletes the
    temporary files and reports `Aborted` with the failure; a failure after it stops the process, as today.
  - `Prepare`, the two-phase path: deletes the temporary files and votes `ForceRollback`, saying the files commit only as
    a transaction's one durable participant.
  - `Rollback`: deletes the temporary files.
  - Its resource manager's identifier is a fixed value of the state store's. It is never reenlisted: no recovery.
- **The session's commit no longer commits the files** in this mode: the files have committed by then. With the opt-out,
  the session commits them as today.
- **The check, in Compze.Internals.SystemCE, internal to Compze's libraries:** a record of the durable resources taking
  part in each transaction, by its local identifier, dropped when the transaction completes. Compze's connection pool
  records a database connection when it opens one in a transaction. The files record themselves when they enlist, as a
  resource that can't share a transaction with another. Whichever records second while the other is there throws.
- **The exception's message**, in substance: "This transaction holds a state store kept in files, in `<folder>`, and a
  database connection, `<connection type>`. A crash between their commits would lose the files' change, or, in an
  endpoint with exactly-once delivery, deliver a tevent about a change that was lost, or count a tessage as handled while
  its change was lost. Compze can't make them commit as one. Keep them in separate units of work, or, if that loss is
  acceptable, declare it on the store's files: `files => files.AllowedInTransactionsThatAlsoWriteADatabase()`."

## Specifications

1. A unit of work that reads a taggregate of the tevent store and changes one of a state store kept in files is refused,
   with the message above.
2. A unit of work that changes a state store's taggregate first and only then opens the tevent store's connection, at its
   own prepare, is refused too.
3. With the opt-out, both units of work commit, both stores' changes are kept, and the files' journal works as today.
4. A unit of work that only reads the state store's taggregates and writes the tevent store commits.
5. A state store kept in files and another durable participant in one transaction, as SQL Server's connection would be,
   is refused, by .NET, and the files' temporary files are gone.
6. A failure while writing the journal rolls the unit of work back: no file changed, the store holds the committed
   instances as before, and a later unit of work succeeds. Specified only if the public API can drive such a failure,
   such as another program taking away the store's own folder between the prepare and the commit; if it can't, the
   builder says so rather than reaching into the internals.
7. Everything a state store kept in files specified before still holds.
