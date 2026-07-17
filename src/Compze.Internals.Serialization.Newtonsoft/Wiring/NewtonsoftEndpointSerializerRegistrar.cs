using Compze.Tessaging.Endpoints;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftEndpointSerializerRegistrar
{
   extension(EndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's serializer: Newtonsoft.Json — filling the one serializer parameter every endpoint<br/>
      /// takes (<see cref="EndpointBuilder.Serializer"/>), covering everything the endpoint sends and receives, of every<br/>
      /// tessage kind.</summary>
      public void NewtonsoftSerializer() => @this.Serializer(registrar => registrar.NewtonsoftTessagingSerializer()
                                                                                   .NewtonsoftTypermediaSerializer());
   }
}
