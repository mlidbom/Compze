using Compze.TypeIdentifiers;
using Compze.Must;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

// Test types used in mapping tests
public class TestEntity;
public class AnotherTestEntity;
// ReSharper disable once UnusedTypeParameter Empty marker type, generic only so the specs can exercise generic-type handling via typeof(); the parameter is intentionally unused.
public class TestGenericEntity<T>;

public class TypeNameMapper_specification
{
   // Stable GUIDs for testing
   static readonly Guid TestEntityGuid = Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
   static readonly Guid AnotherTestEntityGuid = Guid.Parse("f5a9d1b3-8c4e-4a2f-b7d6-3e1c9f0a5b8d");
   static readonly Guid TestGenericEntityGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

   static TypeNameMapper CreateMapper(
      Dictionary<Type, Guid>? leafMappings = null,
      Dictionary<Type, Guid>? openGenericMappings = null,
      HashSet<string>? stableAssemblies = null)
   {
      leafMappings ??= new Dictionary<Type, Guid>
      {
         [typeof(TestEntity)] = TestEntityGuid,
         [typeof(AnotherTestEntity)] = AnotherTestEntityGuid
      };

      openGenericMappings ??= new Dictionary<Type, Guid>
      {
         [typeof(TestGenericEntity<>)] = TestGenericEntityGuid
      };

      stableAssemblies ??= ["System.Private.CoreLib"];

      var mapper = new TypeNameMapper();

      foreach(var kvp in leafMappings)
         mapper.AddLeafTypeMapping(kvp.Key, kvp.Value);

      foreach(var kvp in openGenericMappings)
         mapper.AddOpenGenericMapping(kvp.Key, kvp.Value);

      foreach(var name in stableAssemblies)
         mapper.AddStableAssemblyName(name);

      return mapper;
   }

   public class GetId_for_mapped_leaf_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_MappedTypeIdentifier()
         => (_mapper.GetId(typeof(TestEntity)) is MappedTypeIdentifier).Must().BeTrue();

      [XF] public void has_correct_guid()
         => ((MappedTypeIdentifier)_mapper.GetId(typeof(TestEntity))).GuidValue.Must().Be(TestEntityGuid);

      [XF] public void string_representation_is_guid_comma_zero()
         => _mapper.GetId(typeof(TestEntity)).StringRepresentation.Must().Be($"{TestEntityGuid}, 0");
   }

   public class GetId_for_stable_leaf_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_StableLeafTypeIdentifier()
         => (_mapper.GetId(typeof(string)) is StableLeafTypeIdentifier).Must().BeTrue();

      [XF] public void string_representation_is_the_normalized_short_name()
         => _mapper.GetId(typeof(string)).StringRepresentation.Must().Be("System.String, System.Private.CoreLib");
   }

   public class GetId_for_stable_generic : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_StableGenericTypeIdentifier_for_List_of_string()
         => (_mapper.GetId(typeof(List<string>)) is StableGenericTypeIdentifier).Must().BeTrue();

      [XF] public void string_representation_is_the_normalized_short_name()
         => _mapper.GetId(typeof(List<string>)).StringRepresentation.Must().Be("System.Collections.Generic.List`1[[System.String, System.Private.CoreLib]], System.Private.CoreLib");
   }

   public class GetId_for_generic_with_mapped_argument : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_StableGenericTypeIdentifier()
         => (_mapper.GetId(typeof(List<TestEntity>)) is StableGenericTypeIdentifier).Must().BeTrue();

      [XF] public void string_representation_contains_guid()
         => _mapper.GetId(typeof(List<TestEntity>)).StringRepresentation.Must().Contain(TestEntityGuid.ToString());

      [XF] public void string_representation_contains_list_type_name()
         => _mapper.GetId(typeof(List<TestEntity>)).StringRepresentation.Must().Contain("System.Collections.Generic.List`1");

      [XF] public void string_representation_contains_zero_assembly()
         => _mapper.GetId(typeof(List<TestEntity>)).StringRepresentation.Must().Contain(", 0");
   }

   public class GetId_for_mapped_open_generic_with_mapped_argument : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_MappedGenericTypeIdentifier()
         => (_mapper.GetId(typeof(TestGenericEntity<TestEntity>)) is MappedGenericTypeIdentifier).Must().BeTrue();

      [XF] public void string_representation_contains_definition_guid()
         => _mapper.GetId(typeof(TestGenericEntity<TestEntity>)).StringRepresentation.Must().Contain(TestGenericEntityGuid.ToString());

      [XF] public void string_representation_contains_argument_guid()
         => _mapper.GetId(typeof(TestGenericEntity<TestEntity>)).StringRepresentation.Must().Contain(TestEntityGuid.ToString());
   }

   public class GetId_for_array_of_mapped_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_ArrayTypeIdentifier()
         => (_mapper.GetId(typeof(TestEntity[])) is ArrayTypeIdentifier).Must().BeTrue();

      [XF] public void string_representation_has_array_brackets()
         => _mapper.GetId(typeof(TestEntity[])).StringRepresentation.Must().Contain("[]");

      [XF] public void string_representation_has_entity_guid()
         => _mapper.GetId(typeof(TestEntity[])).StringRepresentation.Must().Contain(TestEntityGuid.ToString());
   }

   public class GetId_for_unmapped_type_throws : TypeNameMapper_specification
   {
      [XF] public void throws_for_unmapped_type_from_non_stable_assembly()
      {
         // Create a mapper that does NOT map TestEntity and does NOT have this assembly as stable
         var mapper = CreateMapper(
            leafMappings: new Dictionary<Type, Guid>(),
            openGenericMappings: new Dictionary<Type, Guid>());

         var threw = false;
         try { mapper.GetId(typeof(TestEntity)); }
         catch(InvalidOperationException) { threw = true; }
         threw.Must().BeTrue();
      }
   }

   public class round_trip_mapped_leaf_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId()
      {
         var typeId = _mapper.GetId(typeof(TestEntity));
         _mapper.GetType(typeId).Must().Be(typeof(TestEntity));
      }
   }

   public class round_trip_stable_leaf_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId()
      {
         var typeId = _mapper.GetId(typeof(string));
         _mapper.GetType(typeId).Must().Be(typeof(string));
      }
   }

   public class round_trip_stable_generic : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId_for_List_of_string()
      {
         var typeId = _mapper.GetId(typeof(List<string>));
         _mapper.GetType(typeId).Must().Be(typeof(List<string>));
      }

      [XF] public void GetType_reverses_GetId_for_Dictionary_of_string_int()
      {
         var typeId = _mapper.GetId(typeof(Dictionary<string, int>));
         _mapper.GetType(typeId).Must().Be(typeof(Dictionary<string, int>));
      }
   }

   public class round_trip_generic_with_mapped_argument : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId_for_List_of_TestEntity()
      {
         var typeId = _mapper.GetId(typeof(List<TestEntity>));
         _mapper.GetType(typeId).Must().Be(typeof(List<TestEntity>));
      }

      [XF] public void GetType_reverses_GetId_for_Dictionary_of_string_TestEntity()
      {
         var typeId = _mapper.GetId(typeof(Dictionary<string, TestEntity>));
         _mapper.GetType(typeId).Must().Be(typeof(Dictionary<string, TestEntity>));
      }
   }

   public class round_trip_mapped_open_generic_with_mapped_argument : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId()
      {
         var typeId = _mapper.GetId(typeof(TestGenericEntity<TestEntity>));
         _mapper.GetType(typeId).Must().Be(typeof(TestGenericEntity<TestEntity>));
      }
   }

   public class round_trip_array_of_mapped_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId()
      {
         var typeId = _mapper.GetId(typeof(TestEntity[]));
         _mapper.GetType(typeId).Must().Be(typeof(TestEntity[]));
      }
   }

   public class round_trip_nested_generic : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void GetType_reverses_GetId_for_Dictionary_of_string_List_of_TestEntity()
      {
         var typeId = _mapper.GetId(typeof(Dictionary<string, List<TestEntity>>));
         _mapper.GetType(typeId).Must().Be(typeof(Dictionary<string, List<TestEntity>>));
      }
   }

   public class persisted_string_round_trip : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void serialize_then_deserialize_mapped_leaf()
      {
         var persisted = _mapper.GetPersistedStringFromAssemblyQualifiedName(typeof(TestEntity).AssemblyQualifiedName!);
         var resolved = _mapper.GetTypeFromPersistedString(persisted);
         resolved.Must().Be(typeof(TestEntity));
      }

      [XF] public void serialize_then_deserialize_stable_leaf()
      {
         var persisted = _mapper.GetPersistedStringFromAssemblyQualifiedName(typeof(string).AssemblyQualifiedName!);
         var resolved = _mapper.GetTypeFromPersistedString(persisted);
         resolved.Must().Be(typeof(string));
      }

      [XF] public void serialize_then_deserialize_generic_with_mapped_arg()
      {
         var persisted = _mapper.GetPersistedStringFromAssemblyQualifiedName(typeof(List<TestEntity>).AssemblyQualifiedName!);
         var resolved = _mapper.GetTypeFromPersistedString(persisted);
         resolved.Must().Be(typeof(List<TestEntity>));
      }

      [XF] public void serialize_then_deserialize_nested_generic()
      {
         var persisted = _mapper.GetPersistedStringFromAssemblyQualifiedName(typeof(Dictionary<string, List<TestEntity>>).AssemblyQualifiedName!);
         var resolved = _mapper.GetTypeFromPersistedString(persisted);
         resolved.Must().Be(typeof(Dictionary<string, List<TestEntity>>));
      }

      [XF] public void serialize_then_deserialize_array_of_mapped_type()
      {
         var persisted = _mapper.GetPersistedStringFromAssemblyQualifiedName(typeof(TestEntity[]).AssemblyQualifiedName!);
         var resolved = _mapper.GetTypeFromPersistedString(persisted);
         resolved.Must().Be(typeof(TestEntity[]));
      }

      [XF] public void serialize_then_deserialize_mapped_open_generic()
      {
         var persisted = _mapper.GetPersistedStringFromAssemblyQualifiedName(typeof(TestGenericEntity<TestEntity>).AssemblyQualifiedName!);
         var resolved = _mapper.GetTypeFromPersistedString(persisted);
         resolved.Must().Be(typeof(TestGenericEntity<TestEntity>));
      }
   }

   // Rename-safety is the library's core promise: a persisted type string must encode mapped components by
   // their stable GUID, never by their renameable assembly-qualified name. Otherwise renaming/moving the
   // mapped type later makes the already-persisted string unresolvable. This must hold no matter how deeply
   // the mapped component is nested — including inside a jagged array.
   public class persisting_an_array_of_a_generic_that_has_a_mapped_type_argument : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();
      string Persist(Type type) => _mapper.GetPersistedStringFromAssemblyQualifiedName(type.AssemblyQualifiedName!);

      public class that_is_single_dimensional : persisting_an_array_of_a_generic_that_has_a_mapped_type_argument
      {
         readonly string _persisted;
         public that_is_single_dimensional() => _persisted = Persist(typeof(List<TestEntity>[]));

         [XF] public void the_mapped_type_argument_is_persisted_as_its_guid()
            => _persisted.Must().Contain(TestEntityGuid.ToString());

         [XF] public void the_persisted_string_does_not_contain_the_mapped_types_renameable_name()
            => _persisted.Must().NotContain(nameof(TestEntity));
      }

      public class that_is_jagged : persisting_an_array_of_a_generic_that_has_a_mapped_type_argument
      {
         readonly string _persisted;
         public that_is_jagged() => _persisted = Persist(typeof(List<TestEntity>[][]));

         [XF] public void the_mapped_type_argument_is_persisted_as_its_guid()
            => _persisted.Must().Contain(TestEntityGuid.ToString());

         [XF] public void the_persisted_string_does_not_contain_the_mapped_types_renameable_name()
            => _persisted.Must().NotContain(nameof(TestEntity));
      }

      public class that_is_jagged_with_mixed_ranks : persisting_an_array_of_a_generic_that_has_a_mapped_type_argument
      {
         readonly string _persisted;
         public that_is_jagged_with_mixed_ranks() => _persisted = Persist(typeof(List<TestEntity>[,][]));

         [XF] public void the_mapped_type_argument_is_persisted_as_its_guid()
            => _persisted.Must().Contain(TestEntityGuid.ToString());

         [XF] public void the_persisted_string_does_not_contain_the_mapped_types_renameable_name()
            => _persisted.Must().NotContain(nameof(TestEntity));
      }
   }
}
