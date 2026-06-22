using Tevent = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Tessaging.Teventive.TeventStore.MySql;

partial class MySqlTeventStoreSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public const string SchemaCreationSql = $"""

                                              CREATE TABLE IF NOT EXISTS {Tevent.TableName}
                                              (
                                                {Tevent.InsertionOrder}       bigint                     NOT NULL  AUTO_INCREMENT,
                                                {Tevent.TaggregateId}          {MySqlGuidType}            NOT NULL,  
                                                {Tevent.UtcTimeStamp}         datetime(6) NOT            NULL,   
                                                {Tevent.TeventType}            int                       NOT NULL,
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

                                              """;

   public void SetupSchemaIfDatabaseUnInitialized() => _schemaManager.EnsureSchemaInitialized();
}
