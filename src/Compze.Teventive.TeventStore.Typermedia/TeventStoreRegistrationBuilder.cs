using Compze.Tessaging.Endpoints;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Wiring;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting;

namespace Compze.Teventive.TeventStore.Typermedia;

public static class TeventStoreTypermediaRegistrar
{
   public static TeventStoreRegistrationBuilder RegisterTeventStore(this ExactlyOnceEndpointBuilder @this)
   {
      @this.MapTypes( mapper =>
      {
         mapper.MapTypesFromAssemblyContaining<TeventStoreApi>();
         mapper.MapTypesFromAssemblyContaining<TeventCache>();
      });
      @this.Registrar.TeventStore(@this.Configuration.ConnectionStringName);
      return new TeventStoreRegistrationBuilder(register => @this.RegisterTessageHandlers(register));
   }
}

public class TeventStoreRegistrationBuilder
{
   readonly Action<Action<TessageHandlerRegistrar>> _registerTessageHandlers;
   internal TeventStoreRegistrationBuilder(Action<Action<TessageHandlerRegistrar>> registerTessageHandlers) => _registerTessageHandlers = registerTessageHandlers;

   ///<summary>Declares the tevent store's typermedia handlers for <typeparamref name="TTaggregate"/> — save, get-for-update, the<br/>
   /// readonly-copy tueries, and the history tuery — into the endpoint's one engine: a store integration is a handler<br/>
   /// contributor like any other, declaring through the same surface as the application's own handlers.</summary>
   public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>()
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      _registerTessageHandlers(TeventStoreApi.RegisterHandlersForTaggregate<TTaggregate, TTevent>);
      return this;
   }
}
