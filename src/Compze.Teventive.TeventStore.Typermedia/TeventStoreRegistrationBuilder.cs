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
      TeventStoreBuilder? teventStore = null;
      @this.Registrar.RequireMappedTypesFromAssemblyContaining<TeventStoreApi>()
                     .TeventStore(@this.Configuration.ConnectionStringName, declare: it => teventStore = it);
      return new TeventStoreRegistrationBuilder(register => @this.RegisterTypermediaHandlers(register), teventStore!);
   }
}

public class TeventStoreRegistrationBuilder
{
   readonly Action<Action<TypermediaHandlerRegistrar>> _registerTypermediaHandlers;
   readonly TeventStoreBuilder _teventStore;

   ///<remarks><paramref name="teventStore"/> declares the store's classes until the container is built, when the store is.</remarks>
   internal TeventStoreRegistrationBuilder(Action<Action<TypermediaHandlerRegistrar>> registerTypermediaHandlers, TeventStoreBuilder teventStore)
   {
      _registerTypermediaHandlers = registerTypermediaHandlers;
      _teventStore = teventStore;
   }

   ///<summary>Declares that the tevent store keeps <typeparamref name="TTaggregate"/>, and its typermedia handlers for it — save,<br/>
   /// get-for-update, the readonly-copy tueries, and the history tuery — into the endpoint's one engine: a store integration is a<br/>
   /// handler contributor like any other, declaring through the same surface as the application's own handlers.</summary>
   public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>()
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : class, ITaggregateTeventWithIdentity
   {
      _teventStore.Taggregate<TTaggregate>();
      _registerTypermediaHandlers(TeventStoreApi.RegisterHandlersForTaggregate<TTaggregate, TTevent>);
      return this;
   }
}
