using Compze.TypeIdentifiers;
using Compze.Must;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

// Test types used in mapping tests
public class TestEntity;
public class AnotherTestEntity;
public class TestGenericEntity<T>;

public class TypeNameMapper_specification
{
   // Stable GUIDs for testing
   static readonly Guid TestEntityGuid = Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
   static readonly Guid AnotherTestEntityGuid = Guid.Parse("f5a9d1b3-8c4e-4a2f-b7d6-3e1c9f0a5b8d");
   static readonly Guid TestGenericEntityGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

   static string TestAssemblyName => typeof(TestEntity).Assembly.GetName().Name!;

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

      [XF] public void returns_MappedTypeId()
         => (_mapper.GetId(typeof(TestEntity)) is MappedTypeId).Must().BeTrue();

      [XF] public void has_correct_guid()
         => ((MappedTypeId)_mapper.GetId(typeof(TestEntity))).GuidValue.Must().Be(TestEntityGuid);

      [XF] public void string_representation_is_guid_comma_zero()
         => _mapper.GetId(typeof(TestEntity)).StringRepresentation.Must().Be($"{TestEntityGuid}, 0");
   }

   public class GetId_for_stable_leaf_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_StableNameTypeId()
         => (_mapper.GetId(typeof(string)) is StableNameTypeId).Must().BeTrue();

      [XF] public void string_representation_is_assembly_qualified_name()
         => _mapper.GetId(typeof(string)).StringRepresentation.Must().Be(typeof(string).AssemblyQualifiedName!);
   }

   public class GetId_for_stable_generic : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_StableNameTypeId_for_List_of_string()
         => (_mapper.GetId(typeof(List<string>)) is StableNameTypeId).Must().BeTrue();

      [XF] public void string_representation_matches_assembly_qualified_name()
         => _mapper.GetId(typeof(List<string>)).StringRepresentation.Must().Be(typeof(List<string>).AssemblyQualifiedName!);
   }

   public class GetId_for_generic_with_mapped_argument : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_ConstructedTypeId()
         => (_mapper.GetId(typeof(List<TestEntity>)) is ConstructedTypeId).Must().BeTrue();

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

      [XF] public void returns_ConstructedTypeId()
         => (_mapper.GetId(typeof(TestGenericEntity<TestEntity>)) is ConstructedTypeId).Must().BeTrue();

      [XF] public void string_representation_contains_definition_guid()
         => _mapper.GetId(typeof(TestGenericEntity<TestEntity>)).StringRepresentation.Must().Contain(TestGenericEntityGuid.ToString());

      [XF] public void string_representation_contains_argument_guid()
         => _mapper.GetId(typeof(TestGenericEntity<TestEntity>)).StringRepresentation.Must().Contain(TestEntityGuid.ToString());
   }

   public class GetId_for_array_of_mapped_type : TypeNameMapper_specification
   {
      readonly TypeNameMapper _mapper = CreateMapper();

      [XF] public void returns_ConstructedTypeId()
         => (_mapper.GetId(typeof(TestEntity[])) is ConstructedTypeId).Must().BeTrue();

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
}
