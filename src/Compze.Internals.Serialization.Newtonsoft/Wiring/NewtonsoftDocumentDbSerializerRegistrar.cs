using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftDocumentDbSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the document db's serializer<br/>
   /// (<see cref="Compze.Abstractions.Serialization.Internal.IDocumentDbSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftDocumentDbSerializer(this IComponentRegistrar registrar) =>
      Private.DocumentDb.NewtonsoftDocumentDbSerializer.RegisterWith(registrar);
}
