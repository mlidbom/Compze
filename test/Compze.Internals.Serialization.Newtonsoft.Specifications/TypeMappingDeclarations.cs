using Compze.TypeIdentifiers;

[assembly: TypeMappings(typeof(Compze.Internals.Serialization.Newtonsoft.Specifications.TypeMappingDeclarations))]

namespace Compze.Internals.Serialization.Newtonsoft.Specifications;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<NewtonSoftTeventStoreTeventSerializerTests.TestTevent>("60c5ec86-b6e5-45e2-828e-8c912b1d1090")
         .Map<When_serializing_polymorphic_wrapper_objects.specifically_entity_ids.PersonId>("27226150-359e-45c7-9881-35879724612b")
         .Map<When_serializing_polymorphic_wrapper_objects.specifically_entity_ids.UserId>("2acb8a85-b9ba-4354-b545-9c6e86c4ece8")
         .Map<When_serializing_types_with_ValueWrapper_members.PersonId>("456222c8-b9e0-4675-abe8-9f7ad5a93718")
         .Map<When_serializing_types_with_ValueWrapper_members.UserId>("555a05ac-d688-4084-a5a3-a86481f94051")
         .Map<OriginalTypes.TypeA>("645544b7-e56c-4e3c-81cd-149e9be90bd7")
         .Map<OriginalTypes.TypeB>("acd2c07a-d3d1-4217-9e71-b13c2775e86d")
         .Map<OriginalTypes.TypeA.TypeAA>("f583784b-29d2-499b-a205-59ea6ef57cb3")
         .Map<OriginalTypes.TypeB.TypeBB>("d65a7c6a-eeb5-485a-a86a-cd4ac8ca99cf")
         .Map<When_serializing_types_with_ValueWrapper_members.User>("fc967965-cab8-466c-b090-b0d9b3555708");
   }
}
