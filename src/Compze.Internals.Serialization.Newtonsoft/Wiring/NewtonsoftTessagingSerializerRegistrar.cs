using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTessagingSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the distributed Tessaging pipeline's serializer<br/>
   /// (<see cref="Compze.Abstractions.Serialization.Internal.ITessagingSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTessagingSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(Private.Tessaging.NewtonsoftTessagingSerializer.RegisterWith);
}
