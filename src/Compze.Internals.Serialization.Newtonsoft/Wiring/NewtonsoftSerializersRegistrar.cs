using Compze.Internals.Serialization.Newtonsoft.Private.DocumentDb;
using Compze.Internals.Serialization.Newtonsoft.Private.Tessaging;
using Compze.Internals.Serialization.Newtonsoft.Private.TeventStore;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftSerializersRegistrar
{
   public static IComponentRegistrar NewtonsoftSerializers(this IComponentRegistrar registrar) =>
      registrar.NewtonSoftRemotableTessageSerializer()
               .NewtonsoftDocumentDbSerializer()
               .NewtonsoftTeventStoreSerializer();
}
