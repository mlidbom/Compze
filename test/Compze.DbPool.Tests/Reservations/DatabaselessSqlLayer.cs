using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;

namespace Compze.DbPool.Tests.Reservations;

///<summary>An <see cref="IDbPoolSqlLayer"/> with no databases behind it: a database's connection string is its name, and resetting one does nothing.<br/>
/// With it a specification of the <see cref="DbPool"/>'s reservations needs no database server.</summary>
///<remarks>The pool keeps its reservations in a machine-wide state named after its SQL layer's type, so each subclass has reservations no other<br/>
/// pool on the machine shares: a specification that gives its pools one of its own knows which databases they will be given.</remarks>
abstract class DatabaselessSqlLayer : IDbPoolSqlLayer
{
   public string ConnectionStringFor(DbPoolDatabase db) => db.Name;
   public void ResetDatabase(DbPoolDatabase db) {}
   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db) {}

   ///<summary>A container with a <see cref="DbPool"/> on <typeparamref name="TSqlLayer"/>.</summary>
   internal static IDependencyInjectionContainer ContainerWithAPoolOn<TSqlLayer>() where TSqlLayer : DatabaselessSqlLayer, new() =>
      DIContainer.Microsoft.CreateTestingContainerBuilder()
                 ._mutate(it => it.Registrar
                                  .DbPool()
                                  .Register(Singleton.For<IDbPoolSqlLayer>().CreatedBy(() => new TSqlLayer())))
                 .Build();
}
