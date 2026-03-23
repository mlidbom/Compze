using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.TypeIdentifiers.Specifications.EndToEndTestMappings))]

namespace Compze.TypeIdentifiers.Specifications;

public class EndToEndTestMappings : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar registrar) =>
      registrar
        .Map<RegistrationTestEntity>("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c")
        .MapOpenGeneric(typeof(RegistrationTestGeneric<>), "a1b2c3d4-e5f6-7890-abcd-ef1234567890");
}
