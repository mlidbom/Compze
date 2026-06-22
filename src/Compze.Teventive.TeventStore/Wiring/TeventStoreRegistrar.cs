using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Internal.Implementation;

namespace Compze.Tessaging.Teventive.TeventStore.Wiring;

public static class TeventStoreRegistrar
{
   static readonly ITeventMigration[] EmptyMigrationsArray = [];

   public static IComponentRegistrar TeventStore(this IComponentRegistrar registrar, string connectionName) =>
      registrar.TeventStore(connectionName, EmptyMigrationsArray);

   static IComponentRegistrar TeventStore(this IComponentRegistrar @this,
                                          string connectionName,
                                          IReadOnlyList<ITeventMigration> migrations) =>
      @this.TeventStoreForFlexibleTesting(connectionName, () => migrations);

   public static IComponentRegistrar TeventStoreForFlexibleTesting(this IComponentRegistrar registrar,
                                                                   string connectionName,
                                                                   Func<IReadOnlyList<ITeventMigration>> migrations)
   {
      Contract.Argument.NotNullEmptyOrWhitespace(connectionName);

      Teventive.TeventStore.TeventStore.RegisterWith(registrar, migrations);

      return registrar.Register(TaggregateTypeValidator.RegisterWith,
                                TeventCache.RegisterWith,
                                TeventStoreUpdater.RegisterWith);
   }
}
