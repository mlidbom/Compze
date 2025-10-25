using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Serialization;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.Tessaging.TyperMediaApi.TeventStore;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.DependencyInjection;

public static class TeventStoreRegistrar
{
   static readonly ITeventMigration[] EmptyMigrationsArray = [];

   public static TeventStoreRegistrationBuilder RegisterTeventStore(this IEndpointBuilder @this) => @this.RegisterTeventStore(EmptyMigrationsArray);

   public static TeventStoreRegistrationBuilder RegisterTeventStore(this IEndpointBuilder @this, IReadOnlyList<ITeventMigration> migrations)
   {
      @this.Container.Register().TeventStore(@this.Configuration.ConnectionStringName, migrations);
      return new TeventStoreRegistrationBuilder(@this.RegisterHandlers);
   }

   public static IComponentRegistrar TeventStore(this IComponentRegistrar registrar, string connectionName) =>
      registrar.TeventStore(connectionName, EmptyMigrationsArray);

   public static IComponentRegistrar TeventStore(this IComponentRegistrar @this,
                                                 string connectionName,
                                                 IReadOnlyList<ITeventMigration> migrations) =>
      @this.TeventStoreForFlexibleTesting(connectionName, () => migrations);

   public static IComponentRegistrar TeventStoreForFlexibleTesting(this IComponentRegistrar registrar,
                                                                   string connectionName,
                                                                   Func<IReadOnlyList<ITeventMigration>> migrations)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(connectionName);

      Teventive.TeventStore.TeventStore.RegisterWith(registrar, migrations);

      return registrar.Register(AggregateTypeValidator.RegisterWith,
                                TeventStoreSerializer.RegisterWith,
                                TeventCache.RegisterWith,
                                TeventStoreUpdater.RegisterWith);
   }
}

public class TeventStoreRegistrationBuilder
{
   readonly TessageHandlerRegistrarWithDependencyInjectionSupport _handlerRegistrar;
   internal TeventStoreRegistrationBuilder(TessageHandlerRegistrarWithDependencyInjectionSupport handlerRegistrar) => _handlerRegistrar = handlerRegistrar;

   public TeventStoreRegistrationBuilder HandleAggregate<TAggregate, TTevent>()
      where TAggregate : class, ITeventStored<TTevent>
      where TTevent : IAggregateTevent
   {
      TeventStoreApi.RegisterHandlersForAggregate<TAggregate, TTevent>(_handlerRegistrar);
      return this;
   }
}
