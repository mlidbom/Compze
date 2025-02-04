﻿using System.Threading.Tasks;
using Compze.Persistence.MsSql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Message =  Compze.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Persistence.MsSql.Messaging.Buses.Implementation;

partial class MsSqlInboxPersistenceLayer
{
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(IMsSqlConnectionPool connectionFactory)
      {
#pragma warning disable CA1849 //The warning is right, this is a blocking call in an async method. But if I make the async call it crashes!
         //Performance: Why is the MessageId not the primary key? Are we worried about performance loss because of fragmentation because of non-sequential Guids? Is there a (performant and truly reliable) sequential-guid-generator we could use? How does it not being the clustered index impact row vs page etc locking?
         // ReSharper disable once MethodHasAsyncOverload | THis crashes with weird exception if called async so ...
         connectionFactory.ExecuteNonQuery($"""

                                            IF NOT EXISTS(select name from sys.tables where name = '{Message.TableName}')
                                            BEGIN
                                                CREATE TABLE {Message.TableName}
                                                (
                                                    {Message.GeneratedId}         bigint IDENTITY(1,1) NOT NULL,
                                                    {Message.TypeId}              uniqueidentifier     NOT NULL,
                                                    {Message.MessageId}           uniqueidentifier     NOT NULL,
                                                    {Message.Status}              smallint             NOT NULL,
                                                    {Message.Body}                nvarchar(MAX)        NOT NULL,
                                                    {Message.ExceptionCount}      int                  NOT NULL  DEFAULT 0,
                                                    {Message.ExceptionType}       nvarchar(500)        NULL,
                                                    {Message.ExceptionStackTrace} nvarchar(MAX)        NULL,
                                                    {Message.ExceptionMessage}    nvarchar(MAX)        NULL,
                                            
                                            
                                                    CONSTRAINT PK_{Message.TableName} PRIMARY KEY CLUSTERED ( [{Message.GeneratedId}] ASC ),
                                            
                                                    CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
                                                )
                                            END

                                            """);
#pragma warning restore
         await Task.CompletedTask.CaF();
      }
   }
}