using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Internals.Transport.TypeMappingDeclarations))]

namespace Compze.Internals.Transport;

#pragma warning disable CA1812 // Instantiated via reflection by StructuralTypeMapper, located via [assembly: TypeMappings]
class TypeMappingDeclarations : ITypeMappingDeclaration
#pragma warning restore CA1812
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<EndpointInformationQuery>("e441c4e2-cc09-4331-9dd9-c9790e72987a")
         .Map<NetworkTopologyQuery>("6fbadb13-f036-4fcb-8a9c-101d74bb76d4");
   }
}
