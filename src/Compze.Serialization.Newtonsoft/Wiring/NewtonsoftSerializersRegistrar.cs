using Compze.Serialization.Newtonsoft.Private.DocumentDb;
using Compze.Serialization.Newtonsoft.Private.Tessaging;
using Compze.Serialization.Newtonsoft.Private.TeventStore;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftSerializersRegistrar
{
   public static IComponentRegistrar NewtonsoftSerializers(this IComponentRegistrar registrar) =>
      registrar.NewtonSoftRemotableTessageSerializer()
               .NewtonsoftDocumentDbSerializer()
               .NewtonsoftTeventStoreSerializer();
}
