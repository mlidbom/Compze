using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Teventive.Internal.Implementation;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Tessaging.TyperMediaApi.EventStore;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.Wiring;

public static class TeventStoreRegistrar
{
   static readonly ITeventMigration[] EmptyMigrationsArray = [];

   public static TeventStoreRegistrationBuilder RegisterTeventStore(this IEndpointBuilder @this) => @this.RegisterTeventStore(EmptyMigrationsArray);

   static TeventStoreRegistrationBuilder RegisterTeventStore(this IEndpointBuilder @this, IReadOnlyList<ITeventMigration> migrations)
   {
      @this.Container.Register().TeventStore(@this.Configuration.ConnectionStringName, migrations);
      return new TeventStoreRegistrationBuilder(@this.RegisterHandlers);
   }

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

public class TeventStoreRegistrationBuilder
{
   readonly TessageHandlerRegistrarWithDependencyInjectionSupport _handlerRegistrar;
   internal TeventStoreRegistrationBuilder(TessageHandlerRegistrarWithDependencyInjectionSupport handlerRegistrar) => _handlerRegistrar = handlerRegistrar;

   public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>()
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      TeventStoreApi.RegisterHandlersForTaggregate<TTaggregate, TTevent>(_handlerRegistrar);
      return this;
   }
}
