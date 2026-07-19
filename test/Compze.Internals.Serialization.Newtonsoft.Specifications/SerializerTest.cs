using Compze.Abstractions.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive.TeventStore.Abstractions.Internal;
using Compze.TypeIdentifiers;

namespace Compze.Internals.Serialization.Newtonsoft.Specifications;

public class SerializerTest : UniversalTestBase
{
   internal ITeventStoreSerializer TeventSerializer => _container.RootResolver.Resolve<ITeventStoreSerializer>();
   internal IDocumentDbSerializer DocumentSerializer => _container.RootResolver.Resolve<IDocumentDbSerializer>();

   readonly IDependencyInjectionContainer _container;

   protected SerializerTest()
   {
      var serializer = PCTSerializerAttribute.Serializer;
      var typeMapper = new TypeMapper();
      typeMapper.MapTypesFromAssemblyContaining<TentityId>();           // Compze.Abstractions — the entity ids these specs serialize
      typeMapper.MapTypesFromAssemblyContaining<IExactlyOnceTevent>();  // Compze.Tessaging.Abstractions — the tessage types these specs serialize
      typeMapper.MapTypesFromAssemblyContaining<AssemblyTypeMapper>();  // this specification assembly — the tevent and document types these specs serialize
#pragma warning disable CA2000 // We are disposing this disposable in DisposeInternal
      _container = DIContainer.Microsoft
                             .CreateTestingContainerBuilder()
                             ._mutate(it => RegisterSerializer(it.Registrar, serializer)
                                              .Register(Singleton.For<ITypeMapper>().Instance(typeMapper))
                                              .Register(Singleton.For<ITypeMap>().Instance(typeMapper)))
                             .Build();
#pragma warning restore CA2000
   }

   static IComponentRegistrar RegisterSerializer(IComponentRegistrar register, Serializer serializer) =>
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
