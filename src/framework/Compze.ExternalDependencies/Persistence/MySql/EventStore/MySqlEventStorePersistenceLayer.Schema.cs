﻿using Compze.Persistence.Common.AdoCE;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.SystemCE.TransactionsCE;
using Event = Compze.Persistence.Common.EventStore.EventTableSchemaStrings;

namespace Compze.Persistence.MySql.EventStore;

partial class MySqlEventStorePersistenceLayer
{
   const string MySqlGuidType = "CHAR(36)";
   bool _initialized;

   public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbient(() =>
   {
      if(!_initialized)
      {
         _connectionManager.UseCommand(suppressTransactionWarning: true,
                                       command => command.ExecuteNonQuery($"""
                                                                           
                                                                           
                                                                           
                                                                               CREATE TABLE IF NOT EXISTS {Event.TableName}
                                                                               (
                                                                                   {Event.InsertionOrder}       bigint                     NOT NULL  AUTO_INCREMENT,
                                                                                   {Event.AggregateId}          {MySqlGuidType}            NOT NULL,  
                                                                                   {Event.UtcTimeStamp}         datetime(6) NOT            NULL,   
                                                                                   {Event.EventType}            {MySqlGuidType}            NOT NULL,    
                                                                                   {Event.Event}                MEDIUMTEXT                 NOT NULL,
                                                                                   {Event.EventId}              {MySqlGuidType}            NOT NULL,
                                                                                   {Event.InsertedVersion}      int                        NOT NULL,
                                                                                   {Event.SqlInsertTimeStamp} TIMESTAMP NOT NULL default CURRENT_TIMESTAMP,
                                                                                   {Event.ReadOrder}            {Event.ReadOrderType}      NOT NULL,    
                                                                                   {Event.EffectiveVersion}     int                        NOT NULL,
                                                                                   {Event.TargetEvent}          {MySqlGuidType}            NULL,
                                                                                   {Event.RefactoringType}      tinyint                    NULL,
                                                                           
                                                                                   PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),
                                                                           
                                                                           
                                                                           
                                                                           
                                                                           
                                                                                   UNIQUE INDEX IX_{Event.TableName}_Unique_{Event.EventId}        ( {Event.EventId} ASC ),
                                                                                   UNIQUE INDEX IX_{Event.TableName}_Unique_{Event.InsertionOrder} ( {Event.InsertionOrder} ASC ),
                                                                                   UNIQUE INDEX IX_{Event.TableName}_Unique_{Event.ReadOrder}      ( {Event.ReadOrder} ASC ),
                                                                           
                                                                                   FOREIGN KEY ( {Event.TargetEvent} ) 
                                                                                       REFERENCES {Event.TableName} ({Event.EventId}),
                                                                           
                                                                           
                                                                                   INDEX IX_{Event.TableName}_{Event.ReadOrder} 
                                                                                       ({Event.ReadOrder} ASC, {Event.EffectiveVersion} ASC)
                                                                                       /*INCLUDE ({Event.EventType}, {Event.InsertionOrder})*/
                                                                           
                                                                               )
                                                                           ENGINE = InnoDB
                                                                           DEFAULT CHARACTER SET = utf8mb4;

                                                                           """));

         _initialized = true;
      }
   });
}