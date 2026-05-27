using Schema = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema;

namespace Compze.Internals.Sql.MicrosoftSql.Private.TypeIdInterning;

partial class MsSqlTypeIdInternerPersistence
{
   // SQL Server caps an index key at 900 bytes. nvarchar is UTF-16 (2 bytes/char) because type strings are
   // NOT guaranteed ASCII (C# identifiers may be Unicode), so 900 / 2 = 450 chars is the unique-index ceiling.
   const int TypeStringMaxLength = 450;

   public static readonly string SchemaCreationSql =
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{Schema.TableName}')
       BEGIN
           CREATE TABLE {Schema.TableName}
           (
               {Schema.Id}         int IDENTITY(1,1)        NOT NULL,
               {Schema.TypeString} nvarchar({TypeStringMaxLength}) NOT NULL,

               CONSTRAINT PK_{Schema.TableName} PRIMARY KEY CLUSTERED ({Schema.Id} ASC),
               CONSTRAINT UQ_{Schema.TableName}_{Schema.TypeString} UNIQUE ({Schema.TypeString})
           )
       END

       """;
}
