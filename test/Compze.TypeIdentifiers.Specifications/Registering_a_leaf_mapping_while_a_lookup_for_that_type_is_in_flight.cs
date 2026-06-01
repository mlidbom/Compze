using System.Reflection;
using Compze.Must;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

// A type whose assembly is treated as stable, so a lookup performed before any GUID mapping is registered
// resolves to a stable identifier (rather than throwing). That stable identifier is exactly what the race
// must not make permanent once a GUID mapping is registered.
public class EntityMappedConcurrentlyWithALookup;

/// <summary>
/// Exposes the lost-update race in <see cref="TypeNameMapper"/> described in
/// <c>dev_docs/TypeId/code-review-findings.md</c> claim #2.
///
/// <para><see cref="TypeNameMapper.GetId"/> is <c>_typeToIdentifier.GetOrAdd(type, ComputeId)</c>. The
/// <c>GetOrAdd</c> factory (<c>ComputeId</c>) reads the mapping dictionaries <b>outside any lock</b>, then the
/// produced value is stored. <c>AddLeafTypeMapping</c> mutates the mapping dictionaries and then calls
/// <c>ClearCaches()</c>. If an in-flight lookup observes the pre-mapping state, a registration then runs its
/// <c>ClearCaches()</c>, and only afterwards the in-flight lookup stores its now-stale result, that stale
/// identifier sticks in the cache permanently — every later lookup returns the unmapped/stable identifier
/// instead of the GUID.</para>
///
/// <para>The interleaving is forced deterministically with an <see cref="IThreadGate"/>:
/// <see cref="TypeWhoseAssemblyQualifiedNameAccessIsGated"/> calls <see cref="IThreadGateVisitor.AwaitPassThrough"/>
/// from inside the <c>AssemblyQualifiedName</c> getter — the last member <c>ComputeId</c> reads on the stable
/// path, after it has already observed that no GUID mapping exists. With the gate closed the lookup parks there
/// while the registration runs, then is released to store its result. This is a behavioural specification, not
/// a test of the current internals: a correct implementation may close the window with a generation check
/// (recompute the stale store) or by serializing registration against in-flight lookups (the registration
/// simply finishes after the lookup is released and its <c>ClearCaches()</c> wipes the stale entry). Under either
/// fix a subsequent lookup returns the mapped identifier and the specification passes; only the lost-update bug
/// leaves it stale.</para>
/// </summary>
public class Registering_a_leaf_mapping_while_a_lookup_for_that_type_is_in_flight
{
   static readonly Guid MappedGuid = Guid.Parse("c0ffee01-dead-beef-cafe-000000000002");

   readonly TypeIdentifier _identifierReturnedByASubsequentLookup;

   public Registering_a_leaf_mapping_while_a_lookup_for_that_type_is_in_flight()
   {
      var mapper = new TypeNameMapper();
      mapper.AddStableAssemblyName(typeof(EntityMappedConcurrentlyWithALookup).Assembly.GetName().Name!);

      var lookupParkedMidComputeId = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "lookupParkedMidComputeId");
      var registrationReachedItsEnd = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "registrationReachedItsEnd");

      var racingType = new TypeWhoseAssemblyQualifiedNameAccessIsGated(
         typeof(EntityMappedConcurrentlyWithALookup),
         onFirstAssemblyQualifiedNameAccess: () => lookupParkedMidComputeId.AwaitPassThrough());

      using var runner = TestingTaskRunner.WithTimeout(WaitTimeout.Seconds(30));

      // The in-flight lookup parks itself inside ComputeId — after it has observed that no GUID mapping exists,
      // and before its result is cached.
      var inFlightLookup = runner.Run(() => mapper.GetId(racingType));
      lookupParkedMidComputeId.AwaitQueueLengthEqualTo(1);

      // The registration runs on its own thread so an implementation that blocks it until the parked lookup
      // completes cannot deadlock this harness.
      runner.Run(() =>
      {
         mapper.AddLeafTypeMapping(racingType, MappedGuid); // mutates the mappings, then calls ClearCaches()
         registrationReachedItsEnd.AwaitPassThrough();
      });

      // Lock-free implementation: the registration's ClearCaches() lands here, while the lookup is still parked —
      // the exact lost update. An implementation that serializes registration against in-flight lookups stays
      // blocked; we release the lookup anyway and the registration finishes behind it.
      registrationReachedItsEnd.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2));

      lookupParkedMidComputeId.Open();
      inFlightLookup.Wait();

      _identifierReturnedByASubsequentLookup = mapper.GetId(racingType);
   }

   [XF] public void a_subsequent_lookup_returns_a_mapped_identifier()
      => (_identifierReturnedByASubsequentLookup is MappedTypeIdentifier).Must().BeTrue();

   [XF] public void a_subsequent_lookup_returns_the_mapped_guid_canonical_string()
      => _identifierReturnedByASubsequentLookup.StringRepresentation.Must().Be($"{MappedGuid}, 0");
}

/// <summary>
/// A <see cref="TypeDelegator"/> that forwards everything to the wrapped type, except that the first read of
/// <see cref="AssemblyQualifiedName"/> invokes the supplied callback before returning. This is a precise,
/// deterministic seam to drive an <see cref="IThreadGate"/> from inside <c>ComputeId</c> — after it has observed
/// the pre-mapping state but before its result is cached — so a concurrent registration can race in.
/// </summary>
sealed class TypeWhoseAssemblyQualifiedNameAccessIsGated : TypeDelegator
{
   readonly Action _onFirstAssemblyQualifiedNameAccess;
   bool _alreadyAccessed;

   public TypeWhoseAssemblyQualifiedNameAccessIsGated(Type delegatingType, Action onFirstAssemblyQualifiedNameAccess)
      : base(delegatingType)
      => _onFirstAssemblyQualifiedNameAccess = onFirstAssemblyQualifiedNameAccess;

   public override string? AssemblyQualifiedName
   {
      get
      {
         if(!_alreadyAccessed)
         {
            _alreadyAccessed = true;
            _onFirstAssemblyQualifiedNameAccess();
         }

         return base.AssemblyQualifiedName;
      }
   }
}
