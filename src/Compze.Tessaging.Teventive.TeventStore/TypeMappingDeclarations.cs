using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Tessaging.Teventive.TeventStore.TypeMappingDeclarations))]

namespace Compze.Tessaging.Teventive.TeventStore;

#pragma warning disable CA1812 // Instantiated via reflection by StructuralTypeMapper, located via [assembly: TypeMappings]
class TypeMappingDeclarations : ITypeMappingDeclaration
#pragma warning restore CA1812
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<Refactoring.Migrations.EndOfTaggregateHistoryTeventPlaceHolder>("fa4197d0-747c-4c60-afbc-f978d7ce2487");
   }
}
