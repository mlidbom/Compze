using Compze.Teventive.TeventStore;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(AssemblyTypeMapper))]

namespace Compze.Teventive.TeventStore;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<Refactoring.Migrations.EndOfTaggregateHistoryTeventPlaceHolder>("fa4197d0-747c-4c60-afbc-f978d7ce2487");
   }
}
