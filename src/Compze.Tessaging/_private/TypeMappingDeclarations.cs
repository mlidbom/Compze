using Compze.TypeIdentifiers;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._private.Transport.Advertisement;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging._private.TessagingAssemblyTypeMapper))]

namespace Compze.Tessaging._private;

//The conventional name AssemblyTypeMapper belongs to Compze.Tessaging.Abstractions, which shares this root namespace and is
//visible here through InternalsVisibleTo, so this assembly's mapper carries its assembly's name.
#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class TessagingAssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<EndpointInformationQuery>("e441c4e2-cc09-4331-9dd9-c9790e72987a")
            .Map<EndpointAddress>("f2a3b4c5-d6e7-4f80-91a2-b3c4d5e6f7a8");
}
