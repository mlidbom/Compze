using Compze.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTeventStoreSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the tevent store's serializer<br/>
   /// (<c>ITeventStoreSerializer</c>).</summary>
   public static IComponentRegistrar NewtonsoftTeventStoreSerializer(this IComponentRegistrar registrar) =>
      _private.TeventStore.NewtonsoftTeventStoreSerializer.RegisterWith(registrar);
}
