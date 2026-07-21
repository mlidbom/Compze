using Compze.Abstractions.Serialization.Internal;
using Compze.Hosting.Testing;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive.TeventStore.Abstractions.Internal;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Internals.Serialization.Newtonsoft.InternalSpecifications;

public class SerializerTest : UniversalTestBase
{
   internal ITeventStoreSerializer TeventSerializer => _container.RootResolver.Resolve<ITeventStoreSerializer>();
   internal IDocumentDbSerializer DocumentSerializer => _container.RootResolver.Resolve<IDocumentDbSerializer>();

   readonly IDependencyInjectionContainer _container;

   protected SerializerTest()
   {
      var serializer = PCTSerializerAttribute.Serializer;
#pragma warning disable CA2000 // We are disposing this disposable in DisposeInternal
      _container = DIContainer.Microsoft
                             .CreateTestingContainerBuilder()
                             ._mutate(it => RegisterSerializers(it.Registrar, serializer)
                                              .RequireMappedTypesFromAssemblyContaining<SerializerTest>())
                             .Build();
#pragma warning restore CA2000
   }

   static IComponentRegistrar RegisterSerializers(IComponentRegistrar register, Serializer serializer) =>
      serializer switch
      {
         Serializer.Newtonsoft => register.NewtonsoftTessagingSerializer()
                                          .NewtonsoftTypermediaSerializer()
                                          .NewtonsoftDocumentDbSerializer()
                                          .NewtonsoftTeventStoreSerializer(),
         _ => throw new ArgumentOutOfRangeException(nameof(serializer))
      };

   protected override void DisposeInternal() => _container.Dispose();
}
