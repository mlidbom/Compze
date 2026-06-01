using Types = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Types;
using Strings = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Strings;
using Names = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Names;

namespace Compze.Internals.Sql.Sqlite.Private.TypeIdInterning;

partial class SqliteTypeIdInternerPersistence
{
   // TypeString / CurrentName / FullyQualifiedName are TEXT and never indexed (resolution is by the int Id).
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
