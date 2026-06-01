using Types = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Types;
using Strings = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Strings;
using Names = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema.Names;

namespace Compze.Internals.Sql.MicrosoftSql.Private.TypeIdInterning;

partial class MsSqlTypeIdInternerPersistence
{
   // TypeString / CurrentName / FullyQualifiedName are nvarchar(max): they are never indexed (resolution is by
   // the int Id), so they carry no length ceiling.
   public static readonly string SchemaCreationSql =
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{Types.TableName}')
       BEGIN
           CREATE TABLE {Types.TableName}
           (
               {Types.Id}          int IDENTITY(1,1) NOT NULL,
               {Types.CurrentName} nvarchar(max)     NOT NULL,

               CONSTRAINT PK_{Types.TableName} PRIMARY KEY CLUSTERED ({Types.Id} ASC)
           )
       END

       IF NOT EXISTS(select name from sys.tables where name = '{Strings.TableName}')
       BEGIN
           CREATE TABLE {Strings.TableName}
           (
               {Strings.TypeString}   nvarchar(max) NOT NULL,
               {Strings.TypeId}       int           NOT NULL,
               {Strings.FirstSeenUtc} datetime2     NOT NULL,

               CONSTRAINT FK_{Strings.TableName}_{Types.TableName} FOREIGN KEY ({Strings.TypeId}) REFERENCES {Types.TableName} ({Types.Id}),
               INDEX IX_{Strings.TableName}_{Strings.TypeId} NONCLUSTERED ({Strings.TypeId})
           )
       END

       IF NOT EXISTS(select name from sys.tables where name = '{Names.TableName}')
       BEGIN
           CREATE TABLE {Names.TableName}
           (
               {Names.TypeId}             int           NOT NULL,
               {Names.Seq}                int           NOT NULL,
               {Names.FullyQualifiedName} nvarchar(max) NOT NULL,
               {Names.FirstSeenUtc}       datetime2     NOT NULL,

               CONSTRAINT PK_{Names.TableName} PRIMARY KEY CLUSTERED ({Names.TypeId} ASC, {Names.Seq} ASC),
               CONSTRAINT FK_{Names.TableName}_{Types.TableName} FOREIGN KEY ({Names.TypeId}) REFERENCES {Types.TableName} ({Types.Id})
           )
       END

       """;
}
