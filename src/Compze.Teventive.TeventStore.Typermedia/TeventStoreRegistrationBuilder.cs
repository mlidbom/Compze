using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Typermedia;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Wiring;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Teventive.TeventStore.Typermedia;

public static class TeventStoreTypermediaRegistrar
{
   public static TeventStoreRegistrationBuilder RegisterTeventStore(this ExactlyOnceEndpointBuilder @this)
   {
      //The store's typermedia surface puts its own tessage types on the wire; the store proper requires the rest where it registers.
      @this.Registrar.RequireMappedTypesFromAssemblyContaining<TeventStoreApi>()
                     .TeventStore(@this.Configuration.ConnectionStringName);
      return new TeventStoreRegistrationBuilder(register => @this.RegisterTypermediaHandlers(register));
   }
}

public class TeventStoreRegistrationBuilder
{
   readonly Action<Action<TypermediaHandlerRegistrar>> _registerTypermediaHandlers;
   internal TeventStoreRegistrationBuilder(Action<Action<TypermediaHandlerRegistrar>> registerTypermediaHandlers) => _registerTypermediaHandlers = registerTypermediaHandlers;

   ///<summary>Declares the tevent store's typermedia handlers for <typeparamref name="TTaggregate"/> — save, get-for-update, the<br/>
   /// readonly-copy tueries, and the history tuery — into the endpoint's one engine: a store integration is a handler<br/>
   /// contributor like any other, declaring through the same surface as the application's own handlers.</summary>
   public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>()
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      _registerTypermediaHandlers(TeventStoreApi.RegisterHandlersForTaggregate<TTaggregate, TTevent>);
      return this;
   }
}
