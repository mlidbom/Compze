using Types = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Types;
using Strings = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Strings;
using Names = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Names;

namespace Compze.Internals.Sql.MySql.Private.TypeIdInterning;

partial class MySqlTypeIdInternerPersistence
{
   // TypeString / CurrentName / FullyQualifiedName are MEDIUMTEXT: never indexed (resolution is by the int Id),
   // so they carry no length ceiling.
   public static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Types.TableName}
       (
           {Types.Id}          INT AUTO_INCREMENT NOT NULL,
           {Types.CurrentName} MEDIUMTEXT         NOT NULL,

           PRIMARY KEY ({Types.Id})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       CREATE TABLE IF NOT EXISTS {Strings.TableName}
       (
           {Strings.TypeString}   MEDIUMTEXT NOT NULL,
           {Strings.TypeId}       INT        NOT NULL,
           {Strings.FirstSeenUtc} DATETIME   NOT NULL,

           INDEX IX_{Strings.TableName}_{Strings.TypeId} ({Strings.TypeId}),
           CONSTRAINT FK_{Strings.TableName}_{Types.TableName} FOREIGN KEY ({Strings.TypeId}) REFERENCES {Types.TableName} ({Types.Id})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       CREATE TABLE IF NOT EXISTS {Names.TableName}
       (
           {Names.TypeId}             INT        NOT NULL,
           {Names.Seq}                INT        NOT NULL,
           {Names.FullyQualifiedName} MEDIUMTEXT NOT NULL,
           {Names.FirstSeenUtc}       DATETIME   NOT NULL,

           PRIMARY KEY ({Names.TypeId}, {Names.Seq}),
           CONSTRAINT FK_{Names.TableName}_{Types.TableName} FOREIGN KEY ({Names.TypeId}) REFERENCES {Types.TableName} ({Types.Id})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       """;
}
