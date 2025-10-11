using Compze.Sql.Common;
using Compze.Utilities.SystemCE.TransactionsCE;
using Event = Compze.Tessaging.Teventive.EventStore.EventTableSchemaStrings;

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
                                                                               {Event.ReadOrder}               TEXT                              NOT NULL UNIQUE,    
                                                                               {Event.EffectiveVersion}        INTEGER                           NOT NULL,
                                                                               {Event.TargetEvent}             TEXT                              NULL,
                                                                               {Event.RefactoringType}         INTEGER                           NULL,
                                                                       
                                                                               UNIQUE ({Event.AggregateId}, {Event.InsertedVersion}),
                                                                               FOREIGN KEY ( {Event.TargetEvent} ) 
                                                                                   REFERENCES {Event.TableName} ({Event.EventId})
                                                                           );

                                                                           CREATE INDEX IF NOT EXISTS IX_{Event.TableName}_{Event.ReadOrder} ON {Event.TableName} 
                                                                                   ({Event.ReadOrder} , {Event.EffectiveVersion} );

                                                                           """));

         _initialized = true;
      }
   });
}
