using Compze.Must;

using Compze.xUnitBDD;
using static Compze.Must.MustActions;
using Compze.TypeIdentifiers._private;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.InternalSpecifications;

/// <summary>
/// The frozen behavioural contract for the parser rework: normalization-on-read and reserved-'0'-authoritative
/// mapped/named classification. Any implementation (regex-evolved or tokenizer) must satisfy exactly these.
/// </summary>
public class Parser_normalization_and_classification_contract
{
   const string CoreLib = "System.Private.CoreLib";

   // §1.6: assembly Version/Culture/PublicKeyToken qualifiers are stripped on read, so the canonical string is
   // the short-name form. This makes identity stable across runtime upgrades (the Version changes, identity
   // does not) and makes the same type — arriving as a full AQN or a short name — produce one canonical string.
   public class Normalizing_assembly_qualifiers_on_read : Parser_normalization_and_classification_contract
   {
      [XF] public void a_full_assembly_qualified_name_becomes_the_short_name()
         => TypeIdentifier.Parse(typeof(string).AssemblyQualifiedName!).StringRepresentation
               .Must().Be($"System.String, {CoreLib}");

      [XF] public void the_canonical_string_carries_no_version_qualifier()
         => TypeIdentifier.Parse(typeof(List<string>).AssemblyQualifiedName!).StringRepresentation
               .Must().NotContain("Version=");

      [XF] public void a_different_runtime_version_yields_the_same_canonical_string()
         => TypeIdentifier.Parse("System.String, System.Private.CoreLib, Version=99.9.9.9, Culture=neutral, PublicKeyToken=7cec85d7bea7798e").StringRepresentation
               .Must().Be($"System.String, {CoreLib}");

      [XF] public void the_short_form_and_the_full_form_collapse_to_one_canonical_string()
         => TypeIdentifier.Parse($"System.String, {CoreLib}").StringRepresentation
               .Must().Be(TypeIdentifier.Parse(typeof(string).AssemblyQualifiedName!).StringRepresentation);

      [XF] public void qualifiers_are_stripped_from_nested_generic_arguments_too()
         => TypeIdentifier.Parse(typeof(List<string>).AssemblyQualifiedName!).StringRepresentation
               .Must().Be($"System.Collections.Generic.List`1[[System.String, {CoreLib}]], {CoreLib}");
   }

   // §1.4: the reserved literal '0' in the assembly position is THE discriminator for a mapped component —
   // not "the type name happens to parse as a GUID". Keying on the GUID-shape alone silently discards a real
   // assembly and corrupts data.
   public class Classifying_a_component_by_the_reserved_zero_field : Parser_normalization_and_classification_contract
   {
      const string Guid1 = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c";

      [XF] public void a_dashed_guid_in_the_reserved_zero_assembly_is_mapped()
         => (TypeIdentifier.Parse($"{Guid1}, 0") is MappedTypeIdentifier).Must().BeTrue();

      [XF] public void a_guid_shaped_name_in_a_real_assembly_is_named_not_mapped()
         => (TypeIdentifier.Parse($"{Guid1}, MyRealAssembly") is MappedTypeIdentifier).Must().BeFalse();

      [XF] public void a_guid_shaped_name_in_a_real_assembly_keeps_its_assembly()
         => TypeIdentifier.Parse($"{Guid1}, MyRealAssembly").StringRepresentation
               .Must().Be($"{Guid1}, MyRealAssembly");

      [XF] public void a_non_guid_name_in_the_reserved_zero_assembly_is_rejected()
         => Invoking(() => TypeIdentifier.Parse("System.String, 0")).Must().Throw<FormatException>();

      [XF] public void a_dashless_guid_in_the_reserved_zero_assembly_is_rejected_as_non_canonical()
         => Invoking(() => TypeIdentifier.Parse("e4a8c9f27b3d4f1a9c6e2d8b5a0f3e7c, 0")).Must().Throw<FormatException>();
   }

   // Regression: a legal CLR type whose FullName is exactly 32 hex chars must survive the library's own
   // serialize -> resolve round-trip. Under the old classification it self-corrupted into "GUID, 0".
   public class A_type_whose_name_is_guid_shaped : Parser_normalization_and_classification_contract
   {
      readonly ITypeMap _mapper = new TypeMapBuilder().UseStableNameStrategyForAssemblyContaining<deadbeefdeadbeefdeadbeefdeadbeef>().Build();

      [XF] public void persists_as_a_named_component_keeping_its_assembly()
         => _mapper.GetId(typeof(deadbeefdeadbeefdeadbeefdeadbeef)).CanonicalString
               .Must().Contain(typeof(deadbeefdeadbeefdeadbeefdeadbeef).Assembly.GetName().Name!);

      [XF] public void round_trips_back_to_the_same_type()
      {
         var persisted = _mapper.GetId(typeof(deadbeefdeadbeefdeadbeefdeadbeef)).CanonicalString;
         _mapper.GetId(persisted).Type.Must().Be(typeof(deadbeefdeadbeefdeadbeefdeadbeef));
      }
   }
}
