using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftEndpointSerializerRegistrar
{
   extension<TConcreteBuilder>(EndpointBuilder<TConcreteBuilder> @this) where TConcreteBuilder : EndpointBuilder<TConcreteBuilder>
   {
      ///<summary>Declares the endpoint's serializer: Newtonsoft.Json — filling the one serializer parameter every endpoint<br/>
      /// takes (<see cref="EndpointBuilder{TConcreteBuilder}.Serializer"/>), covering everything the endpoint sends and receives, of every<br/>
      /// tessage kind.</summary>
      public TConcreteBuilder NewtonsoftSerializer() => @this.Serializer(registrar => registrar.NewtonsoftTessagingSerializer()
                                                                                              .NewtonsoftTypermediaSerializer());
   }
}

public static class NewtonsoftTypermediaClientSerializerRegistrar
{
   extension(TypermediaClientBuilder @this)
   {
      ///<summary>Declares the pure client's serializer: Newtonsoft.Json — filling the one serializer parameter the client<br/>
      /// takes (<see cref="TypermediaClientBuilder.Serializer"/>). A pure client converses only in typermedia, so its<br/>
      /// serializer is the typermedia one.</summary>
      public void NewtonsoftSerializer() => @this.Serializer(registrar => registrar.NewtonsoftTypermediaSerializer());
   }
}
