using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTypermediaSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the Typermedia conversation's serializer<br/>
   /// (<see cref="Compze.Abstractions.Serialization.Internal.ITypermediaSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTypermediaSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(Private.Typermedia.NewtonsoftTypermediaSerializer.RegisterWith);
}
