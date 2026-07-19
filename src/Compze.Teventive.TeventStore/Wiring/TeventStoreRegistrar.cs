using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Internal.Implementation;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Teventive.TeventStore.Wiring;

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

      //Everything the store persists needs identity that survives renames: its own stored types, the taggregate tevent
      //hierarchy it stores, and the entity ids those tevents carry.
      registrar.RequireMappedTypesFromAssemblyContaining<Refactoring.Migrations.EndOfTaggregateHistoryTeventPlaceHolder>();

      return registrar.Register(TaggregateTypeValidator.RegisterWith,
                                TeventCache.RegisterWith,
                                TeventStoreUpdater.RegisterWith);
   }
}
