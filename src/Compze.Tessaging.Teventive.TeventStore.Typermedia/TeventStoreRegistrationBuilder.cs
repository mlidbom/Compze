using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tessaging.TyperMediaApi.EventStore;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Tessaging.Teventive.TeventStore.Wiring;

public static class TeventStoreTypermediaRegistrar
{
   public static TeventStoreRegistrationBuilder RegisterTeventStore(this IEndpointBuilder @this)
   {
      @this.Container.Register().TeventStore(@this.Configuration.ConnectionStringName);
      return new TeventStoreRegistrationBuilder(@this.RegisterTypermediaHandlers);
   }
}

public class TeventStoreRegistrationBuilder
{
   readonly TypermediaHandlerRegistrarWithDependencyInjectionSupport _typermediaRegistrar;
   internal TeventStoreRegistrationBuilder(TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar)
   {
      _typermediaRegistrar = typermediaRegistrar;
   }

   public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>()
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      TeventStoreApi.RegisterHandlersForTaggregate<TTaggregate, TTevent>(_typermediaRegistrar);
      return this;
   }
}
