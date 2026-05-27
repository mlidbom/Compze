# TypeIds table — `TypeString` key sizing

The interner persists each canonical `TypeId` string in a per-database `TypeIds` table with a
**unique index** on the `TypeString` column (so insert-or-get can dedupe). That unique index is the
sizing constraint, and it forces a per-engine decision.

## Type strings are NOT necessarily ASCII

Earlier sizing reasoning leaned on "canonical type strings are ASCII, so `varchar(900)` buys 900
characters." **That assumption is wrong.** C# identifiers may legally contain Unicode letters and
digits, so namespaces, type names, and assembly names — and therefore the canonical strings built
from them — can contain non-ASCII characters.

Consequences:

- The column must be a Unicode type (`nvarchar` on SQL Server, `utf8mb4` on MySQL, `text`/`varchar`
  on Postgres/SQLite which are Unicode already). An ASCII-charset column would corrupt or reject a
  valid type name.
- Byte-limited indexes hold **fewer characters than the byte budget**: a multi-byte character eats
  several bytes of the limit. Size the index for the worst case, not for ASCII.

## Per-engine index limits

| Engine | Hard index-key limit | Notes |
| --- | --- | --- |
| SQL Server | 900 bytes | `nvarchar` is UTF-16 → 2 bytes/char → ~450 chars worst case. A `nvarchar(450)` unique index stays inside 900 bytes. Longer strings need a hash column or `varchar(900)`+collation (rejected: not Unicode-safe). |
| MySQL (InnoDB) | 3072 bytes for the index | `utf8mb4` is up to 4 bytes/char → ~768 chars worst case for a single-column unique index. |
| PostgreSQL | ~2704 bytes (btree) practical | `text` is fine until very long; overflow needs a hash index or hashed column. |
| SQLite | none meaningful | `TEXT ... UNIQUE` is unconstrained for our sizes. |

Constructed generics (`TaggregateLink<Account>`) and deep nesting can make these strings long, so the
limits are reachable in principle — not just theoretical. Whichever MVCC engine is implemented must
pick a Unicode column sized for its real index limit and be tested against the live server, **not**
sized as if the strings were ASCII.

## Transaction-mode flag (renamed)

The per-provider capability flag that drives the interner's transaction strategy is named
**`SuppressAmbientTransactionBeforeAllCalls`** on `ITypeIdInternerPersistence`:

- `true` (MVCC engines) — persistence runs in a suppressed scope, so a mapping commits independently
  of the business transaction; the interner caches mappings aggressively.
- `false` (SQLite) — persistence joins the ambient transaction (single-writer engine; a suppressed
  insert on a second connection deadlocks against the business transaction's writer lock).

The interner is split into one class per flag value
(`SuppressedTransactionTypeIdInterner` / `AmbientTransactionTypeIdInterner`); the flag selects the
class at wiring time rather than being re-checked on every call.
