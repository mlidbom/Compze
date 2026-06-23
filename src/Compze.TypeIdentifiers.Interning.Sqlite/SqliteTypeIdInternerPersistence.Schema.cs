using Types = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Types;
using Strings = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Strings;
using Names = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Names;

namespace Compze.TypeIdentifiers.Interning.Sqlite;

partial class SqliteTypeIdInternerPersistence
{
   // CurrentName / FullyQualifiedName are TEXT and never indexed (resolution is by the int Id). TypeString carries
   // a UNIQUE index: unlike the MVCC engines, SQLite holds no cross-process advisory lock over a mint, so the
   // unique index is what guarantees one spelling maps to exactly one id even if two processes mint it at once —
   // the loser's insert fails and its whole (RequiresNew) mint transaction rolls back, leaving no forked identity.
   // FirstSeenUtc is stored as INTEGER (UTC ticks), matching the engine's DateTime parameter handling.
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Types.TableName}
       (
           {Types.Id}          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Types.CurrentName} TEXT NOT NULL
       );

       CREATE TABLE IF NOT EXISTS {Strings.TableName}
       (
           {Strings.TypeString}   TEXT    NOT NULL,
           {Strings.TypeId}       INTEGER NOT NULL REFERENCES {Types.TableName} ({Types.Id}),
           {Strings.FirstSeenUtc} INTEGER NOT NULL
       );

       CREATE UNIQUE INDEX IF NOT EXISTS UIX_{Strings.TableName}_{Strings.TypeString} ON {Strings.TableName} ({Strings.TypeString});

       CREATE INDEX IF NOT EXISTS IX_{Strings.TableName}_{Strings.TypeId} ON {Strings.TableName} ({Strings.TypeId});

       CREATE TABLE IF NOT EXISTS {Names.TableName}
       (
           {Names.TypeId}             INTEGER NOT NULL REFERENCES {Types.TableName} ({Types.Id}),
           {Names.Seq}                INTEGER NOT NULL,
           {Names.FullyQualifiedName} TEXT    NOT NULL,
           {Names.FirstSeenUtc}       INTEGER NOT NULL,

           PRIMARY KEY ({Names.TypeId}, {Names.Seq})
       );

       """;
}
