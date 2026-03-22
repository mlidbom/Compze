using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.Teventive.TeventStore.AssemblyTypeMapper))]

namespace Compze.Tessaging.Teventive.TeventStore;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(ITypeMappingRegistrar map)
   {
      map.Map<Refactoring.Migrations.EndOfTaggregateHistoryTeventPlaceHolder>("fa4197d0-747c-4c60-afbc-f978d7ce2487");
   }
}
