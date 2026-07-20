using Compze.Tessaging.Typermedia.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Typermedia;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTypermediaSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the Typermedia conversation's serializer<br/>
   /// (<see cref="ITypermediaSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTypermediaSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(Private.Typermedia.NewtonsoftTypermediaSerializer.RegisterWith);
}
