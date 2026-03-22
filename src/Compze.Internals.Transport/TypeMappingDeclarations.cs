using Compze.TypeIdentifiers;

[assembly: TypeMappings(typeof(Compze.Internals.Transport.TypeMappingDeclarations))]

namespace Compze.Internals.Transport;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<EndpointInformationQuery>("e441c4e2-cc09-4331-9dd9-c9790e72987a")
         .Map<NetworkTopologyQuery>("6fbadb13-f036-4fcb-8a9c-101d74bb76d4");
   }
}
