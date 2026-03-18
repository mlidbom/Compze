using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Core.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Internals.Serialization.Newtonsoft.Specifications;

public class SerializerTest : UniversalTestBase
{
   internal ITeventStoreSerializer TeventSerializer => _container.ServiceLocator.Resolve<ITeventStoreSerializer>();
   internal IDocumentDbSerializer DocumentSerializer => _container.ServiceLocator.Resolve<IDocumentDbSerializer>();

   readonly ILegacyContainer _container;

   protected SerializerTest()
   {
      var serializer = PCTSerializerAttribute.Serializer;
#pragma warning disable CA2000 // We are disposing this disposable in DisposeInternal
      _container = (ILegacyContainer)DIContainer.Microsoft
                             .CreateEmpty()
                             ._mutate(it => RegisterSerializer(it.Registrar, serializer)
                                              .TypeMapper());
#pragma warning restore CA2000
   }

   static IComponentRegistrar RegisterSerializer(IComponentRegistrar register, Serializer serializer) =>
      serializer switch
      {
         Serializer.Newtonsoft => register.NewtonsoftSerializers(),
         _ => throw new ArgumentOutOfRangeException(nameof(serializer))
      };

   protected override void DisposeInternal() => _container.Dispose();
}
