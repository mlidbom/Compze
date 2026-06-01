using Types = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Types;
using Strings = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Strings;
using Names = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Names;

namespace Compze.Internals.Sql.PostgreSql.Private.TypeIdInterning;

partial class PgSqlTypeIdInternerPersistence
{
   // TypeString / CurrentName / FullyQualifiedName are TEXT: never indexed (resolution is by the int Id), so
   // they carry no length ceiling.
   public static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Types.TableName}
       (
           {Types.Id}          INT GENERATED ALWAYS AS IDENTITY,
           {Types.CurrentName} TEXT NOT NULL,

           PRIMARY KEY ({Types.Id})
       );

       CREATE TABLE IF NOT EXISTS {Strings.TableName}
       (
           {Strings.TypeString}   TEXT        NOT NULL,
           {Strings.TypeId}       INT         NOT NULL REFERENCES {Types.TableName} ({Types.Id}),
           {Strings.FirstSeenUtc} TIMESTAMPTZ NOT NULL
       );

       CREATE INDEX IF NOT EXISTS IX_{Strings.TableName}_{Strings.TypeId} ON {Strings.TableName} ({Strings.TypeId});

       CREATE TABLE IF NOT EXISTS {Names.TableName}
       (
           {Names.TypeId}             INT         NOT NULL REFERENCES {Types.TableName} ({Types.Id}),
           {Names.Seq}                INT         NOT NULL,
           {Names.FullyQualifiedName} TEXT        NOT NULL,
           {Names.FirstSeenUtc}       TIMESTAMPTZ NOT NULL,

           PRIMARY KEY ({Names.TypeId}, {Names.Seq})
       );

       """;
}
