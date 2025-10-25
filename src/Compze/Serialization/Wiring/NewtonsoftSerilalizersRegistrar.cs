using Compze.Serialization.Newtonsoft.DocumentDb;
using Compze.Serialization.Newtonsoft.Tessaging;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Wiring;

static class NewtonsoftSerializersRegistrar
{
   internal static IComponentRegistrar NewtonsoftSerializers(this IComponentRegistrar registrar) =>
      registrar.NewtonSoftRemotableTessageSerializer()
               .NewtonsoftDocumentDbSerializer();
}
