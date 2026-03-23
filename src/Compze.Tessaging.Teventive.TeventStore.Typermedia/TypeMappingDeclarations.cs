using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.Teventive.TeventStore.Typermedia.AssemblyTypeMapper))]

namespace Compze.Tessaging.Teventive.TeventStore.Typermedia;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(ITypeMappingRegistrar map)
   {
      map.MapOpenGeneric(typeof(TeventStoreApi.TueryApi.TaggregateLink<>), "a3b4c5d6-e7f8-4091-a2b3-c4d5e6f7a8b9");
   }
}
