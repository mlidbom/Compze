using Compze.Must;

using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052
#pragma warning disable CS8981 // BDD-style specification class names describe context using lowercase ASCII (e.g. `validation`, `with_invalid_data`); the language-reserved-name risk is acceptable in test code.

namespace Compze.TypeIdentifiers.Specifications;

public class TypeIdentifierMapper_assembly_registration_specification
{
   static bool IsStableTypeString(string persistedTypeString) => !persistedTypeString.Contains(", 0", StringComparison.Ordinal);

   public class auto_detects_microsoft_assemblies : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void System_Private_CoreLib_is_stable()
      {
         var mapper = new TypeMapBuilder().Build();
         // Stable types keep their AssemblyQualifiedName (no ", 0" GUID format)
         IsStableTypeString(mapper.GetId(typeof(string)).CanonicalString).Must().BeTrue();
      }

      [XF] public void System_Collections_Generic_types_are_stable()
      {
         var mapper = new TypeMapBuilder().Build();
         IsStableTypeString(mapper.GetId(typeof(List<string>)).CanonicalString).Must().BeTrue();
      }
   }

   public class UseStableNameStrategyForAssemblyContaining : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void makes_assembly_stable()
      {
         var mapper = new TypeMapBuilder().UseStableNameStrategyForAssemblyContaining<RegistrationTestEntity>().Build();
         IsStableTypeString(mapper.GetId(typeof(RegistrationTestEntity)).CanonicalString).Must().BeTrue();
      }
   }

   public class end_to_end_with_attribute : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void loads_mappings_from_attributed_assembly()
      {
         var mapper = new TypeMapBuilder().MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly).Build();

         // Mapped types produce a GUID-based persisted string
         mapper.GetId(typeof(RegistrationTestEntity)).CanonicalString.Must().Contain(", 0");
      }

      [XF] public void round_trips_mapped_type()
      {
         var mapper = new TypeMapBuilder().MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly).Build();

         var persisted = mapper.GetId(typeof(RegistrationTestEntity)).CanonicalString;
         mapper.GetId(persisted).Type.Must().Be(typeof(RegistrationTestEntity));
      }

      [XF] public void round_trips_generic_with_mapped_argument()
      {
         var mapper = new TypeMapBuilder().MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly).Build();

         var persisted = mapper.GetId(typeof(List<RegistrationTestEntity>)).CanonicalString;
         mapper.GetId(persisted).Type.Must().Be(typeof(List<RegistrationTestEntity>));
      }

      [XF] public void round_trips_mapped_open_generic()
      {
         var mapper = new TypeMapBuilder().MapTypesFromAssembly(typeof(EndToEndTestMappings).Assembly).Build();

         var persisted = mapper.GetId(typeof(RegistrationTestGeneric<RegistrationTestEntity>)).CanonicalString;
         mapper.GetId(persisted).Type.Must().Be(typeof(RegistrationTestGeneric<RegistrationTestEntity>));
      }
   }

   public class rejects_assembly_without_attribute : TypeIdentifierMapper_assembly_registration_specification
   {
      [XF] public void throws_for_assembly_without_TypeMappingsAttribute()
      {
         // System.Private.CoreLib doesn't have our attribute
         var threw = false;
         try { new TypeMapBuilder().MapTypesFromAssembly(typeof(object).Assembly); }
         catch(InvalidOperationException ex) when(ex.Message.Contains(nameof(AssemblyTypeMapperAttribute), StringComparison.Ordinal))
         { threw = true; }
         threw.Must().BeTrue();
      }
   }
}
