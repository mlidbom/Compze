using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Must;
using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.Abstractions.Specifications.Refactoring.Naming;

/// <summary>
/// Tests that <see cref="StructuralTypeMapper"/> can be built from assemblies
/// using [TypeMappings] attribute declarations and produce correct mappings.
/// </summary>
public class StructuralTypeMapper_specification
{
   static StructuralTypeMapper BuildMapper()
      => StructuralTypeMapper.BuildFromAssemblies(typeof(Compze.Abstractions.TypeMappingDeclarations).Assembly);

   public class When_built_from_assembly_with_TypeMappings_attribute : StructuralTypeMapper_specification
   {
      [XF] public void GetId_returns_correct_MappedTypeId_for_leaf_type()
      {
         var mapper = BuildMapper();
         var id = mapper.GetId(typeof(TentityId));
         id.GuidValue.Must().Be(Guid.Parse("a1d63763-f934-493b-ae92-aeb2f15368b7"));
      }

      [XF] public void GetType_resolves_MappedTypeId_back_to_leaf_type()
      {
         var mapper = BuildMapper();
         var id = new MappedTypeId(Guid.Parse("a1d63763-f934-493b-ae92-aeb2f15368b7"));
         mapper.GetType(id).Must().Be(typeof(TentityId));
      }

      [XF] public void TryGetType_returns_true_for_known_id()
      {
         var mapper = BuildMapper();
         var id = new MappedTypeId(Guid.Parse("a1d63763-f934-493b-ae92-aeb2f15368b7"));
         mapper.TryGetType(id, out var type).Must().BeTrue();
         type.Must().Be(typeof(TentityId));
      }

      [XF] public void TryGetType_returns_false_for_unknown_id()
      {
         var mapper = BuildMapper();
         var id = new MappedTypeId(Guid.NewGuid());
         mapper.TryGetType(id, out _).Must().BeFalse();
      }

      [XF] public void GetIdForTypesAssignableTo_returns_matching_leaf_ids()
      {
         var mapper = BuildMapper();
         var ids = mapper.GetIdForTypesAssignableTo(typeof(TentityId));
         // TentityId itself plus TaggregateId (extends TentityId) and TessageId
         ids.Must().Contain(new MappedTypeId(Guid.Parse("a1d63763-f934-493b-ae92-aeb2f15368b7"))); // TentityId
      }

      [XF] public void AssertMappingsExistFor_does_not_throw_for_mapped_types() =>
         BuildMapper().AssertMappingsExistFor([typeof(TentityId), typeof(TaggregateId)]);

      [XF] public void AssertMappingsExistFor_throws_for_unmapped_types()
      {
         var mapper = BuildMapper();
         Assert.Throws<InvalidOperationException>(() => mapper.AssertMappingsExistFor([typeof(string)]));
      }

      [XF] public void ToPersistedTypeString_returns_guid_comma_zero_for_leaf_type()
      {
         var mapper = BuildMapper();
         var result = mapper.ToPersistedTypeString(typeof(TentityId));
         result.Must().Be("a1d63763-f934-493b-ae92-aeb2f15368b7, 0");
      }

      [XF] public void FromPersistedTypeString_round_trips_leaf_type()
      {
         var mapper = BuildMapper();
         var persisted = mapper.ToPersistedTypeString(typeof(TentityId));
         mapper.FromPersistedTypeString(persisted).Must().Be(typeof(TentityId));
      }
   }
}
