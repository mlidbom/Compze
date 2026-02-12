using Tevent = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TeventTableSchemaStrings;
using Lock = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TaggregateLockTableSchemaStrings;

namespace Compze.Sql.PostgreSql.Private.TEventStore;

public partial class PgSqlTeventStoreSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public const string SchemaCreationSql =
      $"""



           CREATE TABLE IF NOT EXISTS {Tevent.TableName}
           (
               {Tevent.InsertionOrder}          bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
               {Tevent.TaggregateId}             {PgSqlGuidType}                     NOT NULL,  
               {Tevent.UtcTimeStamp}            timestamp with time zone            NOT NULL,   
               {Tevent.TeventType}               {PgSqlGuidType}                     NOT NULL,    
               {Tevent.Tevent}                   TEXT                                NOT NULL,
               {Tevent.TeventId}                 {PgSqlGuidType}                     NOT NULL,
               {Tevent.InsertedVersion}         int                                 NOT NULL,
               {Tevent.SqlInsertTimeStamp}      timestamp  with time zone           NOT NULL  default CURRENT_TIMESTAMP,
               {Tevent.ReadOrder}               {Tevent.ReadOrderType}               NOT NULL,    
               {Tevent.EffectiveVersion}        int                                 NOT NULL,
               {Tevent.TargetTevent}             {PgSqlGuidType}                     NULL,
               {Tevent.RefactoringType}         smallint                            NULL,

               PRIMARY KEY ({Tevent.TaggregateId}, {Tevent.InsertedVersion}),





               CONSTRAINT IX_{Tevent.TableName}_Unique_{Tevent.TeventId}        UNIQUE ( {Tevent.TeventId} ),
               CONSTRAINT IX_{Tevent.TableName}_Unique_{Tevent.InsertionOrder} UNIQUE ( {Tevent.InsertionOrder} ),
               CONSTRAINT IX_{Tevent.TableName}_Unique_{Tevent.ReadOrder}      UNIQUE ( {Tevent.ReadOrder} ),

               FOREIGN KEY ( {Tevent.TargetTevent} ) 
                   REFERENCES {Tevent.TableName} ({Tevent.TeventId})
           );

           CREATE INDEX IF NOT EXISTS IX_{Tevent.TableName}_{Tevent.ReadOrder} ON {Tevent.TableName} 
                   ({Tevent.ReadOrder} , {Tevent.EffectiveVersion} );
                   /*INCLUDE ({Tevent.TeventType}, {Tevent.InsertionOrder})*/


           CREATE TABLE IF NOT EXISTS {Lock.TableName}
           (
               {Lock.TaggregateId} {PgSqlGuidType} NOT NULL,
               PRIMARY KEY ( {Lock.TaggregateId} )
           );


       """;

   public void SetupSchemaIfDatabaseUnInitialized() => _schemaManager.EnsureSchemaInitialized();
}
