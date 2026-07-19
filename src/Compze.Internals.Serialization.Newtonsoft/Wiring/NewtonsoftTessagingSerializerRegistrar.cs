using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTessagingSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the Tessaging pipeline's serializer<br/>
   /// (<see cref="ITessagingSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTessagingSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(Private.Tessaging.NewtonsoftTessagingSerializer.RegisterWith);
}
