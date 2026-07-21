using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus._internal;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTessagingSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the Tessaging pipeline's serializer<br/>
   /// (<see cref="ITessagingSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTessagingSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(_private.Tessaging.NewtonsoftTessagingSerializer.RegisterWith);
}
