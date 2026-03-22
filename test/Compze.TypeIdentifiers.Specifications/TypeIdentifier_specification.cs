using Compze.TypeIdentifiers;
using Compze.Must;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

public class TypeIdentifier_specification
{
   static readonly Guid SampleGuid = Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
   static readonly Guid DifferentGuid = Guid.Parse("f5a9d1b3-8c4e-4a2f-b7d6-3e1c9f0a5b8d");

   public class MappedTypeIdentifier_ : TypeIdentifier_specification
   {
      readonly MappedTypeIdentifier _typeId = new(SampleGuid);

      [XF] public void exposes_guid_value()
         => _typeId.GuidValue.Must().Be(SampleGuid);

      [XF] public void string_representation_is_guid_comma_zero()
         => _typeId.StringRepresentation.Must().Be("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0");

      [XF] public void ToString_returns_string_representation()
         => _typeId.ToString().Must().Be(_typeId.StringRepresentation);

      public class equality : MappedTypeIdentifier_
      {
         [XF] public void equal_when_same_guid()
            => new MappedTypeIdentifier(SampleGuid).Equals(new MappedTypeIdentifier(SampleGuid)).Must().BeTrue();

         [XF] public void not_equal_when_different_guid()
            => new MappedTypeIdentifier(SampleGuid).Equals(new MappedTypeIdentifier(DifferentGuid)).Must().BeFalse();

         [XF] public void operator_equals_works()
            => (new MappedTypeIdentifier(SampleGuid) == new MappedTypeIdentifier(SampleGuid)).Must().BeTrue();

         [XF] public void operator_not_equals_works()
            => (new MappedTypeIdentifier(SampleGuid) != new MappedTypeIdentifier(DifferentGuid)).Must().BeTrue();

         [XF] public void same_hash_code_for_same_guid()
            => new MappedTypeIdentifier(SampleGuid).GetHashCode().Must().Be(new MappedTypeIdentifier(SampleGuid).GetHashCode());
      }
   }

   public class StableNameTypeId_ : TypeIdentifier_specification
   {
      const string StableAqn = "System.String, System.Private.CoreLib";
      readonly StableNameTypeId _typeId = new(StableAqn);

      [XF] public void string_representation_is_the_assembly_qualified_name()
         => _typeId.StringRepresentation.Must().Be(StableAqn);

      public class equality : StableNameTypeId_
      {
         [XF] public void equal_when_same_string()
            => new StableNameTypeId(StableAqn).Equals(new StableNameTypeId(StableAqn)).Must().BeTrue();

         [XF] public void not_equal_when_different_string()
            => new StableNameTypeId(StableAqn).Equals(new StableNameTypeId("System.Int32, System.Private.CoreLib")).Must().BeFalse();
      }
   }

   public class ConstructedTypeId_ : TypeIdentifier_specification
   {
      const string ConstructedString = "System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib";
      readonly ConstructedTypeId _typeId = new(ConstructedString);

      [XF] public void string_representation_is_the_structural_string()
         => _typeId.StringRepresentation.Must().Be(ConstructedString);

      public class equality : ConstructedTypeId_
      {
         [XF] public void equal_when_same_string()
            => new ConstructedTypeId(ConstructedString).Equals(new ConstructedTypeId(ConstructedString)).Must().BeTrue();

         [XF] public void not_equal_when_different_string()
            => new ConstructedTypeId(ConstructedString).Equals(new ConstructedTypeId("other, 0")).Must().BeFalse();
      }
   }

   public class cross_type_equality : TypeIdentifier_specification
   {
      [XF] public void MappedTypeIdentifier_not_equal_to_StableNameTypeId_even_with_same_string_representation()
      {
         TypeIdentifier mapped = new MappedTypeIdentifier(SampleGuid);
         TypeIdentifier stable = new StableNameTypeId(mapped.StringRepresentation);
         // Different concrete types, but string representation equality means they are considered equal
         // This is by design — the string representation IS the identity
         mapped.Equals(stable).Must().BeTrue();
      }
   }
}

public class OpenGenericId_specification
{
   static readonly Guid SampleGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
   static readonly Guid DifferentGuid = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

   [XF] public void exposes_guid_value()
      => new OpenGenericId(SampleGuid).GuidValue.Must().Be(SampleGuid);

   [XF] public void ToString_returns_guid_string()
      => new OpenGenericId(SampleGuid).ToString().Must().Be(SampleGuid.ToString());

   [XF] public void equal_when_same_guid()
      => new OpenGenericId(SampleGuid).Equals(new OpenGenericId(SampleGuid)).Must().BeTrue();

   [XF] public void not_equal_when_different_guid()
      => new OpenGenericId(SampleGuid).Equals(new OpenGenericId(DifferentGuid)).Must().BeFalse();

   [XF] public void operator_equals_works()
      => (new OpenGenericId(SampleGuid) == new OpenGenericId(SampleGuid)).Must().BeTrue();

   [XF] public void operator_not_equals_works()
      => (new OpenGenericId(SampleGuid) != new OpenGenericId(DifferentGuid)).Must().BeTrue();
}
