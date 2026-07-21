using System.Reflection;
using Compze.Must;

using Compze.xUnitBDD;
using static Compze.Must.MustActions;
using Compze.TypeIdentifiers._internal;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052
#pragma warning disable CS8981 // BDD-style spec class names describe context using lowercase ASCII.

namespace Compze.TypeIdentifiers.InternalSpecifications;

// Test types — same assembly as the test project, so the registrar accepts them.
public class TypeRegisteredTwice;
// ReSharper disable once UnusedTypeParameter Empty marker type, generic only so the specs can exercise generic-type handling via typeof(); the parameter is intentionally unused.
public class GenericRegisteredTwice<T>;

/// <summary>
/// A type↔GUID mapping is permanent identity. Allowing the same type to be mapped a second time silently
/// rebinds it — already-persisted <c>$type</c> data written under the first GUID then becomes unresolvable,
/// or resolves to the wrong type. So a second registration of an already-mapped type must throw, loudly,
/// at registration time rather than corrupt data later. This is the invariant; these specs pin it.
/// </summary>
public class When_a_type_is_already_mapped
{
   static readonly Assembly TestAssembly = typeof(TypeRegisteredTwice).Assembly;
   const string FirstId = "11111111-1111-1111-1111-111111111111";
   const string DifferentId = "22222222-2222-2222-2222-222222222222";

   public class as_a_leaf_type : When_a_type_is_already_mapped
   {
      readonly IAssemblyTypeMappingRegistrar _registrar = new AssemblyTypeMappingRegistrar(TestAssembly).Map<TypeRegisteredTwice>(FirstId);

      [XF] public void mapping_it_again_to_a_different_id_throws_InvalidOperationException()
         => Invoking(() => _registrar.Map<TypeRegisteredTwice>(DifferentId)).Must().Throw<InvalidOperationException>();

      [XF] public void mapping_it_again_to_the_same_id_throws_InvalidOperationException()
         => Invoking(() => _registrar.Map<TypeRegisteredTwice>(FirstId)).Must().Throw<InvalidOperationException>();

      [XF] public void the_thrown_exception_names_the_duplicated_type()
         => Invoking(() => _registrar.Map<TypeRegisteredTwice>(DifferentId)).Must().Throw<InvalidOperationException>()
            .Which.Message.Must().Contain(nameof(TypeRegisteredTwice));
   }

   public class as_an_open_generic_type : When_a_type_is_already_mapped
   {
      readonly IAssemblyTypeMappingRegistrar _registrar = new AssemblyTypeMappingRegistrar(TestAssembly).MapOpenGeneric(typeof(GenericRegisteredTwice<>), FirstId);

      [XF] public void mapping_it_again_to_a_different_id_throws_InvalidOperationException()
         => Invoking(() => _registrar.MapOpenGeneric(typeof(GenericRegisteredTwice<>), DifferentId)).Must().Throw<InvalidOperationException>();

      [XF] public void mapping_it_again_to_the_same_id_throws_InvalidOperationException()
         => Invoking(() => _registrar.MapOpenGeneric(typeof(GenericRegisteredTwice<>), FirstId)).Must().Throw<InvalidOperationException>();

      [XF] public void the_thrown_exception_names_the_duplicated_type()
         => Invoking(() => _registrar.MapOpenGeneric(typeof(GenericRegisteredTwice<>), DifferentId)).Must().Throw<InvalidOperationException>()
            .Which.Message.Must().Contain(nameof(GenericRegisteredTwice<int>));
   }
}
