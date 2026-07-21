using Compze.Abstractions;
using Compze.Must;

using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

/// <summary>
/// Tests that an <see cref="ITypeMap"/> can be built from assemblies
/// using [TypeMappings] attribute declarations and produce correct mappings.
/// </summary>
public class TypeIdentifierMapper_specification
{
   static ITypeMap BuildMapper() => new TypeMapBuilder().MapTypesFromAssembly(typeof(TentityId).Assembly).Build();

   public class When_built_from_assembly_with_TypeMappings_attribute : TypeIdentifierMapper_specification
   {
      [XF] public void GetId_returns_canonical_guid_comma_zero_for_leaf_type()
      {
         var mapper = BuildMapper();
         var id = mapper.GetId(typeof(TentityId));
         id.CanonicalString.Must().Be("a1d63763-f934-493b-ae92-aeb2f15368b7, 0");
      }

      [XF] public void GetId_resolved_type_is_the_leaf_type()
      {
         var mapper = BuildMapper();
         mapper.GetId(typeof(TentityId)).Type.Must().Be(typeof(TentityId));
      }

      [XF] public void GetId_from_persisted_string_resolves_leaf_guid_back_to_type()
      {
         var mapper = BuildMapper();
         mapper.GetId("a1d63763-f934-493b-ae92-aeb2f15368b7, 0").Type.Must().Be(typeof(TentityId));
      }

      [XF] public void GetId_from_persisted_string_throws_for_unknown_guid()
      {
         var mapper = BuildMapper();
         Assert.Throws<InvalidOperationException>(() => mapper.GetId($"{Guid.NewGuid()}, 0"));
      }

      [XF] public void AssertMappingsExistFor_does_not_throw_for_mapped_types() =>
         BuildMapper().AssertMappingsExistFor([typeof(TentityId), typeof(TaggregateId)]);

      [XF] public void AssertMappingsExistFor_throws_for_unmapped_types()
      {
         var mapper = BuildMapper();
         // Use a type from a non-stable assembly that has no mapping declared
         Assert.Throws<InvalidOperationException>(() => mapper.AssertMappingsExistFor([typeof(TypeIdentifierMapper_specification)]));
      }

      [XF] public void GetId_CanonicalString_returns_guid_comma_zero_for_leaf_type()
      {
         var mapper = BuildMapper();
         var result = mapper.GetId(typeof(TentityId)).CanonicalString;
         result.Must().Be("a1d63763-f934-493b-ae92-aeb2f15368b7, 0");
      }

      [XF] public void GetId_round_trips_leaf_type_through_persisted_string()
      {
         var mapper = BuildMapper();
         var persisted = mapper.GetId(typeof(TentityId)).CanonicalString;
         mapper.GetId(persisted).Type.Must().Be(typeof(TentityId));
      }
   }
}
