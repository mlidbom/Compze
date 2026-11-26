using Compze.DependencyInjection.Abstractions;
using Compze.Sql.PostgreSql.Wiring._internal;

namespace Compze.Sql.PostgreSql.Wiring;

public static class PgSqlDomainDatabaseRegistrar
{
   ///<summary>Declares the domain database this endpoint joins: the PostgreSQL database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql layer the endpoint registers stores its data through. Each sql layer contributes<br/>
   /// its own schema, created before the database's first use, and demands the type-id interner it shares with the others itself —<br/>
   /// this one declaration is all the persistence wiring an endpoint needs before its sql layers register.</summary>
   ///<remarks>A transaction takes the database's connection as its one durable resource, whose commit is the transaction's decision: the<br/>
   /// database commits once every other participant of the transaction has prepared, and before any is told the outcome. So a<br/>
   /// transaction can't also hold another durable resource, such as another database's connection or a resource manager of your own.<br/>
   /// When that takes part first, the write to this database throws, saying so. When it takes part second, .NET refuses it itself:<br/>
   /// "Implicit distributed transactions have not been enabled".<br/>
   /// With distributed transactions enabled, the connection refuses to prepare a two-phase commit, and the transaction rolls back.<br/>
   /// Keep each database, and each other durable resource, in transactions of its own.</remarks>
   ///<remarks>On Linux, a synchronous call to the database waits for a free thread-pool thread to receive its answer: once anything has<br/>
   /// used a connection asynchronously, as Compze's asynchronous paths do, .NET completes a synchronous read of its socket on a pool<br/>
   /// thread. So a process whose pool threads block, such as on synchronous units of work waiting for one another's locks, can stall<br/>
   /// its own database calls for seconds, on Linux only: Windows blocks such a read for real. There, run such work asynchronously, as<br/>
   /// with <c>ExecuteUnitOfWorkAsync</c>.</remarks>
   public static IComponentRegistrar PgSqlDomainDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
}
