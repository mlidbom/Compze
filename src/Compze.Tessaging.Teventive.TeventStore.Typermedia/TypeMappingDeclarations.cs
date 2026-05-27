using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Tessaging.Teventive.TeventStore.Typermedia.TypeMappingDeclarations))]

namespace Compze.Tessaging.Teventive.TeventStore.Typermedia;

#pragma warning disable CA1812 // Instantiated via reflection by StructuralTypeMapper, located via [assembly: TypeMappings]
class TypeMappingDeclarations : ITypeMappingDeclaration
#pragma warning restore CA1812
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.MapOpenGeneric(typeof(TeventStoreApi.TueryApi.TaggregateLink<>), "a3b4c5d6-e7f8-4091-a2b3-c4d5e6f7a8b9");
   }
}
