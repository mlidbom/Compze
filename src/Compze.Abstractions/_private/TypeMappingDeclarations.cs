using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Abstractions._private.AssemblyTypeMapper))]

namespace Compze.Abstractions._private;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<TentityId>("a1d63763-f934-493b-ae92-aeb2f15368b7")
         .Map<TaggregateId>("5d87bfa3-5f88-4d3b-8971-c994757286ce");
   }
}
