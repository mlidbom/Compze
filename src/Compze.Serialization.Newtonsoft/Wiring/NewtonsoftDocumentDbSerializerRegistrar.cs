using Compze.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftDocumentDbSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the document db's serializer<br/>
   /// (<see cref="Compze.Abstractions.Serialization._internal.IDocumentDbSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftDocumentDbSerializer(this IComponentRegistrar registrar) =>
      _private.DocumentDb.NewtonsoftDocumentDbSerializer.RegisterWith(registrar);
}
