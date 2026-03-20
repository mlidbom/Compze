using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Tessaging.Teventive.TeventStore.TypeMappingDeclarations))]

namespace Compze.Tessaging.Teventive.TeventStore;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<Refactoring.Migrations.EndOfTaggregateHistoryTeventPlaceHolder>("fa4197d0-747c-4c60-afbc-f978d7ce2487");
   }
}
