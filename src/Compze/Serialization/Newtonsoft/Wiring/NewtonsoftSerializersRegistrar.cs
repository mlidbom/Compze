using Compze.Serialization.Newtonsoft.Private.DocumentDb;
using Compze.Serialization.Newtonsoft.Private.Tessaging;
using Compze.Serialization.Newtonsoft.Private.TeventStore;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Wiring;

static class NewtonsoftSerializersRegistrar
{
   internal static IComponentRegistrar NewtonsoftSerializers(this IComponentRegistrar registrar) =>
      registrar.NewtonSoftRemotableTessageSerializer()
               .NewtonsoftDocumentDbSerializer()
               .NewtonsoftTeventStoreSerializer();
}
