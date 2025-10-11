using Compze.Sql.Common;
using Compze.Utilities.SystemCE.TransactionsCE;
using Event = Compze.Tessaging.Teventive.EventStore.EventTableSchemaStrings;
using Lock = Compze.Tessaging.Teventive.EventStore.AggregateLockTableSchemaStrings;

namespace Compze.Tessaging.Teventive.EventStore.Sqlite;

partial class SqliteEventStoreSqlLayer
{
   bool _initialized;

   public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbient(() =>
   {
      if(!_initialized)
      {
         _connectionManager.UseCommand(suppressTransactionWarning: true,
                                       command => command.ExecuteNonQuery($"""

                                                                           CREATE TABLE IF NOT EXISTS {Event.TableName}
                                                                           (
                                                                               {Event.InsertionOrder}          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                                                               {Event.AggregateId}             TEXT                              NOT NULL,  
                                                                               {Event.UtcTimeStamp}            TEXT                              NOT NULL,   
                                                                               {Event.EventType}               TEXT                              NOT NULL,    
                                                                               {Event.Event}                   TEXT                              NOT NULL,
                                                                               {Event.EventId}                 TEXT                              NOT NULL UNIQUE,
                                                                               {Event.InsertedVersion}         INTEGER                           NOT NULL,
                                                                               {Event.SqlInsertTimeStamp}      TEXT                              NOT NULL DEFAULT (datetime('now')),
                                                                               {Event.ReadOrderIntegerPart}    INTEGER                           NOT NULL,    
                                                                               {Event.ReadOrderFractionPart}   INTEGER                           NOT NULL,    
                                                                               {Event.EffectiveVersion}        INTEGER                           NOT NULL,
                                                                               {Event.TargetEvent}             TEXT                              NULL,
                                                                               {Event.RefactoringType}         INTEGER                           NULL,
                                                                       
                                                                               UNIQUE ({Event.AggregateId}, {Event.InsertedVersion}),
                                                                               UNIQUE ({Event.ReadOrderIntegerPart}, {Event.ReadOrderFractionPart}),
                                                                               FOREIGN KEY ( {Event.TargetEvent} ) 
                                                                                   REFERENCES {Event.TableName} ({Event.EventId})
                                                                           );

                                                                           CREATE INDEX IF NOT EXISTS IX_{Event.TableName}_{Event.ReadOrderIntegerPart}_{Event.ReadOrderFractionPart} ON {Event.TableName} 
                                                                                   ({Event.ReadOrderIntegerPart}, {Event.ReadOrderFractionPart}, {Event.EffectiveVersion} );

                                                                           CREATE TABLE IF NOT EXISTS {Lock.TableName}
                                                                           (
                                                                               {Lock.AggregateId} TEXT NOT NULL,
                                                                               PRIMARY KEY ( {Lock.AggregateId} )
                                                                           );

                                                                           """));

         _initialized = true;
      }
   });
}

