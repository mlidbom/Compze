using Compze.Sql.Common;
using Compze.Utilities.SystemCE.TransactionsCE;
using Tevent = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Sql.MySql.TEventStore;

partial class MySqlTeventStoreSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";
   bool _initialized;

   public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbient(() =>
   {
      if(!_initialized)
      {
         _connectionManager.UseCommand(suppressTransactionWarning: true,
                                       command => command.ExecuteNonQuery($"""
                                                                           
                                                                           
                                                                           
                                                                               CREATE TABLE IF NOT EXISTS {Tevent.TableName}
                                                                               (
                                                                                   {Tevent.InsertionOrder}       bigint                     NOT NULL  AUTO_INCREMENT,
                                                                                   {Tevent.TaggregateId}          {MySqlGuidType}            NOT NULL,  
                                                                                   {Tevent.UtcTimeStamp}         datetime(6) NOT            NULL,   
                                                                                   {Tevent.TeventType}            {MySqlGuidType}            NOT NULL,    
                                                                                   {Tevent.Tevent}                MEDIUMTEXT                 NOT NULL,
                                                                                   {Tevent.TeventId}              {MySqlGuidType}            NOT NULL,
                                                                                   {Tevent.InsertedVersion}      int                        NOT NULL,
                                                                                   {Tevent.SqlInsertTimeStamp} TIMESTAMP NOT NULL default CURRENT_TIMESTAMP,
                                                                                   {Tevent.ReadOrder}            {Tevent.ReadOrderType}      NOT NULL,    
                                                                                   {Tevent.EffectiveVersion}     int                        NOT NULL,
                                                                                   {Tevent.TargetTevent}          {MySqlGuidType}            NULL,
                                                                                   {Tevent.RefactoringType}      tinyint                    NULL,
                                                                           
                                                                                   PRIMARY KEY ({Tevent.TaggregateId}, {Tevent.InsertedVersion}),
                                                                           
                                                                           
                                                                           
                                                                           
                                                                           
                                                                                   UNIQUE INDEX IX_{Tevent.TableName}_Unique_{Tevent.TeventId}        ( {Tevent.TeventId} ASC ),
                                                                                   UNIQUE INDEX IX_{Tevent.TableName}_Unique_{Tevent.InsertionOrder} ( {Tevent.InsertionOrder} ASC ),
                                                                                   UNIQUE INDEX IX_{Tevent.TableName}_Unique_{Tevent.ReadOrder}      ( {Tevent.ReadOrder} ASC ),
                                                                           
                                                                                   FOREIGN KEY ( {Tevent.TargetTevent} ) 
                                                                                       REFERENCES {Tevent.TableName} ({Tevent.TeventId}),
                                                                           
                                                                           
                                                                                   INDEX IX_{Tevent.TableName}_{Tevent.ReadOrder} 
                                                                                       ({Tevent.ReadOrder} ASC, {Tevent.EffectiveVersion} ASC)
                                                                                       /*INCLUDE ({Tevent.TeventType}, {Tevent.InsertionOrder})*/
                                                                           
                                                                               )
                                                                           ENGINE = InnoDB
                                                                           DEFAULT CHARACTER SET = utf8mb4;

                                                                           """));

         _initialized = true;
      }
   });
}