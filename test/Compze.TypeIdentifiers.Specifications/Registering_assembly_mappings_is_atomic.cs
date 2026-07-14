using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

public class FirstTypeInAnAtomicBatch;
public class SecondTypeInAnAtomicBatch;

/// <summary>
/// An assembly's mappings are published as one atomic snapshot transition. If any mapping in the batch collides
/// (a reused GUID, or a type mapped twice), the whole batch is rejected and NONE of it is published — so a failed
/// registration leaves the mapper unchanged and retryable, never half-registered.
/// </summary>
public class Registering_assembly_mappings_is_atomic
{
   static readonly Guid GuidA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
   static readonly Guid GuidB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
   static readonly Guid SharedGuid = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

   public class When_a_batch_contains_a_colliding_guid : Registering_assembly_mappings_is_atomic
   {
      readonly TypeNameMapper _mapper = new();

      public When_a_batch_contains_a_colliding_guid()
         => Invoking(() => _mapper.AddAssemblyMappings(
               [new(typeof(FirstTypeInAnAtomicBatch), SharedGuid), new(typeof(SecondTypeInAnAtomicBatch), SharedGuid)], []))
            .Must().Throw<InvalidOperationException>();

      [XF] public void the_mapping_that_came_before_the_collision_is_not_published()
         => _mapper.HasLeafMapping(typeof(FirstTypeInAnAtomicBatch)).Must().BeFalse();

      [XF] public void the_colliding_mapping_is_not_published()
         => _mapper.HasLeafMapping(typeof(SecondTypeInAnAtomicBatch)).Must().BeFalse();

      [XF] public void a_corrected_batch_then_registers_cleanly()
      {
         _mapper.AddAssemblyMappings([new(typeof(FirstTypeInAnAtomicBatch), GuidA), new(typeof(SecondTypeInAnAtomicBatch), GuidB)], []);
         _mapper.HasLeafMapping(typeof(FirstTypeInAnAtomicBatch)).Must().BeTrue();
         _mapper.HasLeafMapping(typeof(SecondTypeInAnAtomicBatch)).Must().BeTrue();
      }
   }
}
