using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Tessaging.TypeMappingDeclarations))]

namespace Compze.Tessaging;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<Compze.Internals.Transport.EndpointInformationQuery>("e441c4e2-cc09-4331-9dd9-c9790e72987a")
         .Map<Compze.Internals.Transport.NetworkTopologyQuery>("6fbadb13-f036-4fcb-8a9c-101d74bb76d4");
   }
}
