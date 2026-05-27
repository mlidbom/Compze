using Schema = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema;

namespace Compze.Internals.Sql.MySql.Private.TypeIdInterning;

partial class MySqlTypeIdInternerPersistence
{
   // InnoDB caps an index key at 3072 bytes. utf8mb4 is up to 4 bytes/char, so 3072 / 4 = 768 chars is the
   // unique-index ceiling. The column is utf8mb4 (NOT ascii) because type strings are not guaranteed ASCII.
   const int TypeStringMaxLength = 768;

   public static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TableName}
       (
       {Schema.Id}         INT AUTO_INCREMENT NOT NULL,
       {Schema.TypeString} VARCHAR({TypeStringMaxLength}) NOT NULL,

       PRIMARY KEY ({Schema.Id}),
       UNIQUE ({Schema.TypeString})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       """;
}
