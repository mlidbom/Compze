using Schema = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema;

namespace Compze.Internals.Sql.Sqlite.Private.TypeIdInterning;

partial class SqliteTypeIdInternerPersistence
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TableName}
       (
           {Schema.Id}         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Schema.TypeString} TEXT NOT NULL UNIQUE
       );

       """;
}
