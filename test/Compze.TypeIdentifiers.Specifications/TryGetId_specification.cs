using Compze.Abstractions.Public;
using Compze.Must;

using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

/// <summary>
/// <see cref="ITypeMap.TryGetId"/> returns the canonical id for any type it can currently map, and false —
/// without throwing — for a type that is neither mapped nor from a stable assembly.
/// </summary>
public class TryGetId_specification
{
   static TypeMapper BuildMapper()
   {
      var mapper = new TypeMapper();
      mapper.MapTypesFromAssembly(typeof(TentityId).Assembly);
      return mapper;
   }

   public class For_a_mapped_type : TryGetId_specification
   {
      [XF] public void returns_true() =>
         BuildMapper().TryGetId(typeof(TentityId), out _).Must().Be(true);

      [XF] public void out_parameter_is_the_canonical_id()
      {
         BuildMapper().TryGetId(typeof(TentityId), out var id);
         id!.CanonicalString.Must().Be("a1d63763-f934-493b-ae92-aeb2f15368b7, 0");
      }
   }

   public class For_a_stable_runtime_type : TryGetId_specification
   {
      [XF] public void returns_true() =>
         BuildMapper().TryGetId(typeof(string), out _).Must().Be(true);
   }

   public class For_an_unmapped_non_stable_type : TryGetId_specification
   {
      [XF] public void returns_false() =>
         BuildMapper().TryGetId(typeof(TryGetId_specification), out _).Must().Be(false);

      [XF] public void leaves_the_out_parameter_null()
      {
         BuildMapper().TryGetId(typeof(TryGetId_specification), out var id);
         Assert.Null(id);
      }
   }
}
