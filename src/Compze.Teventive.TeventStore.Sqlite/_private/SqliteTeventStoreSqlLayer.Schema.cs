using Tevent = Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.Sqlite._private;

partial class SqliteTeventStoreSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Tevent.TableName}
       (
           {Tevent.InsertionOrder}          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Tevent.TaggregateId}            TEXT                              NOT NULL,  
           {Tevent.UtcTimeStamp}            INTEGER                           NOT NULL,   
           {Tevent.TeventType}              INTEGER                           NOT NULL,
           {Tevent.Tevent}                  TEXT                              NOT NULL,
           {Tevent.TeventId}                TEXT                              NOT NULL UNIQUE,
           {Tevent.InsertedVersion}         INTEGER                           NOT NULL,
           {Tevent.SqlInsertTimeStamp}      INTEGER                           NOT NULL DEFAULT 0,
           {Tevent.ReadOrderIntegerPart}    INTEGER                           NOT NULL,    
           {Tevent.ReadOrderFractionPart}   INTEGER                           NOT NULL,    
           {Tevent.EffectiveVersion}        INTEGER                           NOT NULL,
           {Tevent.TargetTevent}            TEXT                              NULL,
           {Tevent.RefactoringType}         INTEGER                           NULL,

           UNIQUE ({Tevent.TaggregateId}, {Tevent.InsertedVersion}),
           UNIQUE ({Tevent.ReadOrderIntegerPart}, {Tevent.ReadOrderFractionPart}),
           FOREIGN KEY ( {Tevent.TargetTevent} ) 
               REFERENCES {Tevent.TableName} ({Tevent.TeventId})
       );

       CREATE INDEX IF NOT EXISTS IX_{Tevent.TableName}_{Tevent.ReadOrderIntegerPart}_{Tevent.ReadOrderFractionPart} ON {Tevent.TableName} 
               ({Tevent.ReadOrderIntegerPart}, {Tevent.ReadOrderFractionPart}, {Tevent.EffectiveVersion} );

       CREATE INDEX IF NOT EXISTS IX_{Tevent.TableName}_{Tevent.TaggregateId} ON {Tevent.TableName} 
               ({Tevent.TaggregateId}, {Tevent.InsertedVersion}, {Tevent.ReadOrderIntegerPart}, {Tevent.ReadOrderFractionPart});

       """;

   public void SetupSchemaIfDatabaseUnInitialized() => _schemaManager.EnsureSchemaInitialized();
}
