namespace Compze.DbPool;

public interface IDbPoolSqlLayer
{
   void ResetDatabase(DbPoolDatabase db);

   string ConnectionStringFor(DbPoolDatabase db);

   void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
   }

   void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db);

   ///<summary>How many databases the pools on a machine may create with this SQL layer, 100 unless the layer says otherwise. Once that many<br/>
   /// exist and all are reserved, a reservation waits for one to be released.</summary>
   ///<remarks>Each database the pools create stays, for later reservations to reuse: on a database server, each one takes the server's disk<br/>
   /// space for good. A test suite that holds more databases at once than this, such as one that runs many tests at once on a machine with<br/>
   /// many processors, waits for them, and its tests time out.</remarks>
   int MaximumNumberOfDatabases
   {
      get
      {
         //review-naming: chosen by Claude, 2026-11-27, for a public member of an alpha package.
         return 100;
      }
   }
}
