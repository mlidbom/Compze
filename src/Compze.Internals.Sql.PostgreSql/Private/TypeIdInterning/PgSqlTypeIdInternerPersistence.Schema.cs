using Schema = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema;

namespace Compze.Internals.Sql.PostgreSql.Private.TypeIdInterning;

partial class PgSqlTypeIdInternerPersistence
{
   // PostgreSQL's btree index limit (~2704 bytes) is generous; 500 UTF-8 chars stay well within it even at
   // 4 bytes/char. Type strings are NOT guaranteed ASCII, but Postgres is UTF-8 throughout so no charset opt-out is needed.
   const int TypeStringMaxLength = 500;

   public static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TableName}
       (
           {Schema.Id}         INT GENERATED ALWAYS AS IDENTITY,
           {Schema.TypeString} VARCHAR({TypeStringMaxLength}) NOT NULL,

           PRIMARY KEY ({Schema.Id}),
           UNIQUE ({Schema.TypeString})
       );

       """;
}
