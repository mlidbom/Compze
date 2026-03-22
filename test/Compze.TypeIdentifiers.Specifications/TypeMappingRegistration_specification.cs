using System.Reflection;
using Compze.TypeIdentifiers;
using Compze.Must;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

// Test types — same assembly as the test project
public class RegistrationTestEntity;
public class RegistrationTestGeneric<T>;

public class TypeMappingRegistrar_specification
{
   static readonly Assembly TestAssembly = typeof(RegistrationTestEntity).Assembly;

   static TypeMappingRegistrar CreateRegistrar() => new(TestAssembly);

   public class Map_leaf_type : TypeMappingRegistrar_specification
   {
      [XF] public void registers_the_mapping()
      {
         var registrar = CreateRegistrar();
         registrar.Map<RegistrationTestEntity>("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
         registrar.LeafTypeMappings.ContainsKey(typeof(RegistrationTestEntity)).Must().BeTrue();
      }

      [XF] public void stores_correct_guid()
      {
         var registrar = CreateRegistrar();
         registrar.Map<RegistrationTestEntity>("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
         registrar.LeafTypeMappings[typeof(RegistrationTestEntity)].Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));
      }

      [XF] public void is_fluent()
      {
         var registrar = CreateRegistrar();
         var result = registrar.Map<RegistrationTestEntity>("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
         ReferenceEquals(result, registrar).Must().BeTrue();
      }
   }

   public class Map_open_generic : TypeMappingRegistrar_specification
   {
      [XF] public void registers_the_mapping()
      {
         var registrar = CreateRegistrar();
         registrar.MapOpenGeneric(typeof(RegistrationTestGeneric<>), "a1b2c3d4-e5f6-7890-abcd-ef1234567890");
         registrar.OpenGenericMappings.ContainsKey(typeof(RegistrationTestGeneric<>)).Must().BeTrue();
      }

      [XF] public void stores_correct_guid()
      {
         var registrar = CreateRegistrar();
         registrar.MapOpenGeneric(typeof(RegistrationTestGeneric<>), "a1b2c3d4-e5f6-7890-abcd-ef1234567890");
         registrar.OpenGenericMappings[typeof(RegistrationTestGeneric<>)].Must().Be(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
      }
   }

   public class validation : TypeMappingRegistrar_specification
   {
      [XF] public void rejects_type_from_different_assembly()
      {
         var registrar = CreateRegistrar();
         var threw = false;
         try { registrar.Map<string>("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"); }
         catch(InvalidOperationException ex) when(ex.Message.Contains("only map its own types"))
         { threw = true; }
         threw.Must().BeTrue();
      }

      [XF] public void rejects_open_generic_in_Map()
      {
         // Can't use Map<T> with an open generic definition since you can't use
         // typeof(MyType<>) as a type parameter. So this path is naturally prevented by the compiler.
         // But the check exists for safety. We test via MapOpenGeneric path with non-generic type.
         var registrar = CreateRegistrar();
         var threw = false;
         try { registrar.MapOpenGeneric(typeof(RegistrationTestEntity), "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"); }
         catch(InvalidOperationException ex) when(ex.Message.Contains("open generic definition"))
         { threw = true; }
         threw.Must().BeTrue();
      }

      [XF] public void rejects_open_generic_from_different_assembly_in_MapOpenGeneric()
      {
         var registrar = CreateRegistrar();
         var threw = false;
         try { registrar.MapOpenGeneric(typeof(List<>), "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"); }
         catch(InvalidOperationException ex) when(ex.Message.Contains("only map its own types"))
         { threw = true; }
         threw.Must().BeTrue();
      }
   }
}

public class TypeIdentifierMapper_assembly_registration_specification
{
   static bool IsStableTypeString(string persistedTypeString) => !persistedTypeString.Contains(", 0");

   public class auto_detects_microsoft_assemblies : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void System_Private_CoreLib_is_stable()
      {
         var mapper = new TypeMapper();
         // Stable types keep their AssemblyQualifiedName (no ", 0" GUID format)
         IsStableTypeString(mapper.ToPersistedTypeString(typeof(string))).Must().BeTrue();
      }

      [XF] public void System_Collections_Generic_types_are_stable()
      {
         var mapper = new TypeMapper();
         IsStableTypeString(mapper.ToPersistedTypeString(typeof(List<string>))).Must().BeTrue();
      }
   }

   public class UseStableNameStrategyForAssemblyContaining : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void makes_assembly_stable()
      {
         var mapper = new TypeMapper();
         mapper.UseStableNameStrategyForAssemblyContaining<RegistrationTestEntity>();
         IsStableTypeString(mapper.ToPersistedTypeString(typeof(RegistrationTestEntity))).Must().BeTrue();
      }
   }

   public class end_to_end_with_attribute : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void loads_mappings_from_attributed_assembly()
      {
         var mapper = new TypeMapper();
         mapper.MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly);

         // Mapped types produce a GUID-based persisted string
         mapper.ToPersistedTypeString(typeof(RegistrationTestEntity)).Must().Contain(", 0");
      }

      [XF] public void round_trips_mapped_type()
      {
         var mapper = new TypeMapper();
         mapper.MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly);

         var persisted = mapper.ToPersistedTypeString(typeof(RegistrationTestEntity));
         mapper.FromPersistedTypeString(persisted).Must().Be(typeof(RegistrationTestEntity));
      }

      [XF] public void round_trips_generic_with_mapped_argument()
      {
         var mapper = new TypeMapper();
         mapper.MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly);

         var persisted = mapper.ToPersistedTypeString(typeof(List<RegistrationTestEntity>));
         mapper.FromPersistedTypeString(persisted).Must().Be(typeof(List<RegistrationTestEntity>));
      }

      [XF] public void round_trips_mapped_open_generic()
      {
         var mapper = new TypeMapper();
         mapper.MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly);

         var persisted = mapper.ToPersistedTypeString(typeof(RegistrationTestGeneric<RegistrationTestEntity>));
         mapper.FromPersistedTypeString(persisted).Must().Be(typeof(RegistrationTestGeneric<RegistrationTestEntity>));
      }
   }

   public class rejects_assembly_without_attribute : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void throws_for_assembly_without_TypeMappingsAttribute()
      {
         // System.Private.CoreLib doesn't have our attribute
         var mapper = new TypeMapper();
         var threw = false;
         try { mapper.MapTypesFromAssembly(typeof(object).Assembly); }
         catch(InvalidOperationException ex) when(ex.Message.Contains(nameof(AssemblyTypeMapperAttribute)))
         { threw = true; }
         threw.Must().BeTrue();
      }
   }
}
