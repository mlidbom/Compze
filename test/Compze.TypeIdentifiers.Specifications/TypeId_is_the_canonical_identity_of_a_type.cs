using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

/// <summary>
/// <see cref="TypeId"/> is the canonical identity of a type, obtained only from an <see cref="ITypeMap"/>: two
/// instances for the same type are equal, share a hash code, and share one canonical string. This is the invariant
/// that lets a <see cref="TypeId"/> be used as a dictionary key and as the persisted <c>$type</c> value. Equality
/// is type identity (reference equality on the resolved <see cref="System.Type"/>), not canonical-string comparison.
/// </summary>
public class TypeId_is_the_canonical_identity_of_a_type
{
   readonly ITypeMap _map = new TypeMapper();

   public class For_the_same_type : TypeId_is_the_canonical_identity_of_a_type
   {
      [XF] public void two_lookups_are_equal()
         => _map.GetId(typeof(string)).Must().Be(_map.GetId(typeof(string)));

      [XF] public void two_lookups_share_a_hash_code()
         => _map.GetId(typeof(string)).GetHashCode().Must().Be(_map.GetId(typeof(string)).GetHashCode());

      [XF] public void two_lookups_share_one_canonical_string()
         => _map.GetId(typeof(string)).CanonicalString.Must().Be(_map.GetId(typeof(string)).CanonicalString);
   }

   public class For_different_types : TypeId_is_the_canonical_identity_of_a_type
   {
      [XF] public void the_ids_are_not_equal()
         => _map.GetId(typeof(string)).Equals(_map.GetId(typeof(int))).Must().BeFalse();
   }

   public class For_the_full_and_the_short_form_of_one_stable_type : TypeId_is_the_canonical_identity_of_a_type
   {
      [XF] public void both_persisted_strings_resolve_to_equal_ids()
         => _map.GetId(typeof(string).AssemblyQualifiedName!)
               .Must().Be(_map.GetId("System.String, System.Private.CoreLib"));

      [XF] public void the_id_carries_the_normalized_short_canonical_string()
         => _map.GetId(typeof(string).AssemblyQualifiedName!).CanonicalString
               .Must().Be("System.String, System.Private.CoreLib");
   }
}
