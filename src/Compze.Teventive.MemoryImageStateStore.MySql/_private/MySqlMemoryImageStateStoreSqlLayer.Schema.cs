using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.MySql._private;

partial class MySqlMemoryImageStateStoreSqlLayer
{
   ///<summary>The table of the taggregates' locks, a row for each taggregate ever locked: see <see cref="LockAndReadUnlessHeldAs"/>.</summary>
   internal const string TaggregateLockTable = "MemoryImageStateStoreTaggregateLock";

   internal static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TaggregateTable}
       (
           {Schema.Id}           CHAR(36)     NOT NULL,
           {Schema.ClassName}    VARCHAR({Schema.ClassNameLength}) NOT NULL,
           {Schema.Snapshot}     LONGTEXT     NULL,
           {Schema.CommitNumber} BIGINT       NOT NULL,

           PRIMARY KEY ({Schema.Id}),
           INDEX IX_{Schema.TaggregateTable}_{Schema.CommitNumber} ({Schema.CommitNumber})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       CREATE TABLE IF NOT EXISTS {Schema.CommitCounterTable}
       (
           {Schema.Id}               INT    NOT NULL,
           {Schema.LastCommitNumber} BIGINT NOT NULL,

           PRIMARY KEY ({Schema.Id})
       )
       ENGINE = InnoDB;

       INSERT IGNORE INTO {Schema.CommitCounterTable} ({Schema.Id}, {Schema.LastCommitNumber}) VALUES ({Schema.TheCounterRow}, 0);

       CREATE TABLE IF NOT EXISTS {TaggregateLockTable}
       (
           {Schema.Id} CHAR(36) NOT NULL,

           PRIMARY KEY ({Schema.Id})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       """;
}
