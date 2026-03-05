using Tevent = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Internals.Sql.MicrosoftSql.Private.TEventStore;

partial class MsSqlTeventStoreSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{Tevent.TableName}')
       BEGIN
           CREATE TABLE {Tevent.TableName}
           (
               {Tevent.InsertionOrder}       bigint IDENTITY(1,1)               NOT NULL,
               {Tevent.TaggregateId}          uniqueidentifier                   NOT NULL,  
               {Tevent.UtcTimeStamp}         datetime2                          NOT NULL,   
               {Tevent.TeventType}            uniqueidentifier                   NOT NULL,    
               {Tevent.Tevent}                nvarchar(max)                      NOT NULL,
               {Tevent.TeventId}              uniqueidentifier                   NOT NULL,
               {Tevent.InsertedVersion}      int                                NOT NULL,
               {Tevent.SqlInsertTimeStamp}   datetime2 default SYSUTCDATETIME() NOT NULL,
               {Tevent.ReadOrder}            {Tevent.ReadOrderType}              NOT NULL,    
               {Tevent.EffectiveVersion}     int                                NOT NULL,
               {Tevent.TargetTevent}          uniqueidentifier                   NULL,
               {Tevent.RefactoringType}      tinyint                            NULL,

               CONSTRAINT PK_{Tevent.TableName} PRIMARY KEY CLUSTERED 
               (
                   {Tevent.TaggregateId} ASC,
                   {Tevent.InsertedVersion} ASC
               )WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

               CONSTRAINT IX_{Tevent.TableName}_Unique_{Tevent.TeventId}        UNIQUE ( {Tevent.TeventId} ),
               CONSTRAINT IX_{Tevent.TableName}_Unique_{Tevent.InsertionOrder} UNIQUE ( {Tevent.InsertionOrder} ),
               CONSTRAINT IX_{Tevent.TableName}_Unique_{Tevent.ReadOrder}      UNIQUE ( {Tevent.ReadOrder} ),

               CONSTRAINT FK_{Tevent.TableName}_{Tevent.TargetTevent} FOREIGN KEY ( {Tevent.TargetTevent} ) 
                   REFERENCES {Tevent.TableName} ({Tevent.TeventId}) 
           )

               CREATE NONCLUSTERED INDEX IX_{Tevent.TableName}_{Tevent.ReadOrder} ON {Tevent.TableName}
                   ({Tevent.ReadOrder}, {Tevent.EffectiveVersion})
                   INCLUDE ({Tevent.TeventType}, {Tevent.InsertionOrder})
       END 

       """;

   public void SetupSchemaIfDatabaseUnInitialized() => _schemaManager.EnsureSchemaInitialized();
}
