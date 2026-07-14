using Compze.Teventive.TeventStore.Typermedia;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(AssemblyTypeMapper))]

namespace Compze.Teventive.TeventStore.Typermedia;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.MapOpenGeneric(typeof(TeventStoreApi.TueryApi.TaggregateLink<>), "a3b4c5d6-e7f8-4091-a2b3-c4d5e6f7a8b9");
   }
}
