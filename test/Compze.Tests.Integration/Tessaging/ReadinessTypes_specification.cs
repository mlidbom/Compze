using Compze.Abstractions.Hosting.Public;
using Compze.Must;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>A <see cref="ReadinessTypes"/> set contains only remotable single-handler tessage types — tueries, typermedia<br/>
/// tommands, exactly-once tommands — the kinds for which "a handler is available" means anything. Anything else, and a set<br/>
/// that would be empty, fails loud at composition — never as a patience-exhausted timeout later.</summary>
public class ReadinessTypes_specification
{
   [XF] public void an_explicit_set_containing_a_tevent_type_fails_loud_naming_the_type_and_the_allowed_kinds() =>
      Invoking(() => ReadinessTypes.These(typeof(MyBestEffortTevent)))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain(nameof(MyBestEffortTevent))
         .Contain("not a remotable single-handler tessage type");

   [XF] public void an_explicit_empty_set_fails_loud() =>
      Invoking(() => ReadinessTypes.These([]))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("Awaiting readiness for nothing");

   [XF] public void a_reflected_assembly_set_contains_the_assemblys_single_handler_types_and_nothing_else()
   {
      var types = ReadinessTypes.InAssemblyContaining<MyExactlyOnceTommandHandledOnlyByTheLateEndpoint>().Types;

      types.Must().Contain(typeof(MyExactlyOnceTommandHandledOnlyByTheLateEndpoint));
      types.Must().Contain(typeof(MyTueryHandledOnlyByTheLateEndpoint));
      //A tevent in the same assembly is filtered out, never an error: it is simply not a readiness concern.
      types.Contains(typeof(CQRS.MigratedBeforeUserRegisteredTevent)).Must().BeFalse();
   }

   [XF] public void a_reflected_assembly_set_that_would_be_empty_fails_loud() =>
      //System.Private.CoreLib contains no tessage types at all.
      Invoking(() => ReadinessTypes.InAssemblyContaining<object>())
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("contains no remotable single-handler tessage types");

   [XF] public void a_namespace_set_contains_the_namespace_subtrees_single_handler_types()
   {
      var types = ReadinessTypes.InNamespaceOf<MyExactlyOnceTommandHandledOnlyByTheLateEndpoint>().Types;

      types.Must().Contain(typeof(MyExactlyOnceTommandHandledOnlyByTheLateEndpoint));
      types.Must().Contain(typeof(MyTueryHandledOnlyByTheLateEndpoint));
   }

   [XF] public void walking_up_namespace_levels_widens_the_subtree() =>
      ReadinessTypes.InNamespaceOf<MyExactlyOnceTommandHandledOnlyByTheLateEndpoint>(levelsToWalkUpBeforeRecursingDown: 1).Types.Count
                    .Must().BeGreaterThanOrEqualTo(ReadinessTypes.InNamespaceOf<MyExactlyOnceTommandHandledOnlyByTheLateEndpoint>().Types.Count);
}
