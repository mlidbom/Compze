using System.Reflection;
using Compze.Must;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052
#pragma warning disable CS8981 // BDD-style spec class names describe context using lowercase ASCII.

namespace Compze.TypeIdentifiers.Specifications;

// Test types — same assembly as the test project, so the registrar accepts them.
public class FirstTypeSharingAGuid;
public class SecondTypeSharingAGuid;
// ReSharper disable UnusedTypeParameter Empty marker types, generic only so the specs can exercise generic-type handling via typeof(); the parameters are intentionally unused.
public class FirstGenericSharingAGuid<T>;
public class SecondGenericSharingAGuid<T>;
// ReSharper restore UnusedTypeParameter

/// <summary>
/// A GUID is permanent, unique type identity. If two different types share a GUID, persisted <c>$type</c> data
/// becomes ambiguous — the GUID resolves to the wrong type and the other type becomes unresolvable. Copy-pasting
/// a mapping line and forgetting to change the GUID is an easy, catastrophic mistake, so a second type claiming
/// an already-used GUID must throw at registration time. These specs pin that.
///
/// <para>The guard is needed at two levels: the per-assembly registrar catches the common copy-paste within a
/// single mapping declaration, at the point of declaration; the merged map (<see cref="TypeNameMapper"/>) is the
/// only place that can catch two separate assemblies independently choosing the same GUID.</para>
/// </summary>
public class When_a_guid_is_already_mapped_to_a_type
{
   static readonly Assembly TestAssembly = typeof(FirstTypeSharingAGuid).Assembly;
   const string SharedId = "33333333-3333-3333-3333-333333333333";

   public class for_a_leaf_type_declared_in_one_assembly_mapping : When_a_guid_is_already_mapped_to_a_type
   {
      readonly IAssemblyTypeMappingRegistrar _registrar = new AssemblyTypeMappingRegistrar(TestAssembly).Map<FirstTypeSharingAGuid>(SharedId);

      [XF] public void mapping_a_second_leaf_type_to_the_same_guid_throws_InvalidOperationException()
         => Invoking(() => _registrar.Map<SecondTypeSharingAGuid>(SharedId)).Must().Throw<InvalidOperationException>();

      [XF] public void the_thrown_exception_names_the_conflicting_guid()
         => Invoking(() => _registrar.Map<SecondTypeSharingAGuid>(SharedId)).Must().Throw<InvalidOperationException>()
            .Which.Message.Must().Contain(SharedId);
   }

   public class for_an_open_generic_declared_in_one_assembly_mapping : When_a_guid_is_already_mapped_to_a_type
   {
      readonly IAssemblyTypeMappingRegistrar _registrar = new AssemblyTypeMappingRegistrar(TestAssembly).MapOpenGeneric(typeof(FirstGenericSharingAGuid<>), SharedId);

      [XF] public void mapping_a_second_open_generic_to_the_same_guid_throws_InvalidOperationException()
         => Invoking(() => _registrar.MapOpenGeneric(typeof(SecondGenericSharingAGuid<>), SharedId)).Must().Throw<InvalidOperationException>();
   }

   public class for_leaf_types_in_the_merged_type_map : When_a_guid_is_already_mapped_to_a_type
   {
      readonly TypeNameMapper _mapper = new();
      public for_leaf_types_in_the_merged_type_map() => _mapper.AddLeafTypeMapping(typeof(FirstTypeSharingAGuid), Guid.Parse(SharedId));

      [XF] public void mapping_a_second_leaf_type_to_the_same_guid_throws_InvalidOperationException()
         => Invoking(() => _mapper.AddLeafTypeMapping(typeof(SecondTypeSharingAGuid), Guid.Parse(SharedId))).Must().Throw<InvalidOperationException>();
   }

   public class for_open_generic_types_in_the_merged_type_map : When_a_guid_is_already_mapped_to_a_type
   {
      readonly TypeNameMapper _mapper = new();
      public for_open_generic_types_in_the_merged_type_map() => _mapper.AddOpenGenericMapping(typeof(FirstGenericSharingAGuid<>), Guid.Parse(SharedId));

      [XF] public void mapping_a_second_open_generic_to_the_same_guid_throws_InvalidOperationException()
         => Invoking(() => _mapper.AddOpenGenericMapping(typeof(SecondGenericSharingAGuid<>), Guid.Parse(SharedId))).Must().Throw<InvalidOperationException>();
   }
}
