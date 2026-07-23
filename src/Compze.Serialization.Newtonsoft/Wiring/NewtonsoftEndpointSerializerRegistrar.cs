using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftEndpointSerializerRegistrar
{
   extension(EndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's serializer: Newtonsoft.Json — filling the one serializer parameter every endpoint<br/>
      /// takes (<see cref="EndpointBuilder.Serializer"/>), covering everything the endpoint sends and receives, of every<br/>
      /// tessage kind.</summary>
      public EndpointBuilder NewtonsoftSerializer() => @this.Serializer(registrar => registrar.NewtonsoftTessagingSerializer()
                                                                                              .NewtonsoftTypermediaSerializer());
   }
}

public static class NewtonsoftTypermediaClientSerializerRegistrar
{
   extension(TypermediaClientBuilder @this)
   {
      ///<summary>Declares the pure client's serializer: Newtonsoft.Json — filling the one serializer parameter the client<br/>
      /// takes (<see cref="TypermediaClientBuilder.ConfigureSerializer"/>). A pure client converses only in typermedia, so its<br/>
      /// serializer is the typermedia one.</summary>
      public void NewtonsoftSerializer() => @this.ConfigureSerializer(registrar => registrar.NewtonsoftTypermediaSerializer());
   }
}
