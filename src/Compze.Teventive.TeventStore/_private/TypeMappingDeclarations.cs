using Compze.Teventive.TeventStore;
using Compze.TypeIdentifiers;
using Compze.Teventive.TeventStore._private;
using Compze.Teventive.TeventStore.Refactoring.Migrations._private;

[assembly: AssemblyTypeMapper(typeof(AssemblyTypeMapper))]

namespace Compze.Teventive.TeventStore._private;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<Refactoring.Migrations._private.EndOfTaggregateHistoryTeventPlaceHolder>("fa4197d0-747c-4c60-afbc-f978d7ce2487");
   }
}
