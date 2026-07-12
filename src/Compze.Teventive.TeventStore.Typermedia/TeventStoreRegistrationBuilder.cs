using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Tessaging.Teventive.TeventStore.Typermedia;

public static class TeventStoreTypermediaRegistrar
{
   public static TeventStoreRegistrationBuilder RegisterTeventStore(this IEndpointBuilder @this)
   {
      @this.TypeMapper.MapTypesFromAssemblyContaining<TeventStoreApi>();
      @this.TypeMapper.MapTypesFromAssemblyContaining<TeventCache>();
      @this.Registrar.TeventStore(@this.Configuration.ConnectionStringName);
      return new TeventStoreRegistrationBuilder(@this.RegisterTypermediaHandlers);
   }
}

public class TeventStoreRegistrationBuilder
{
   readonly TypermediaHandlerRegistrarWithDependencyInjectionSupport _typermediaRegistrar;
   internal TeventStoreRegistrationBuilder(TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar) => _typermediaRegistrar = typermediaRegistrar;

   public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>()
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      TeventStoreApi.RegisterHandlersForTaggregate<TTaggregate, TTevent>(_typermediaRegistrar);
      return this;
   }
}
