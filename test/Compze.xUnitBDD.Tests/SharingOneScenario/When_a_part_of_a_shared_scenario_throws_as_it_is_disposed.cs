using Compze.Must;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_a_part_of_a_shared_scenario_throws_as_it_is_disposed : ISharesOneScenario
{
   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassWhosePartThrowsAsItIsDisposed)));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassWhosePartThrowsAsItIsDisposed : ISharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _first = ISharesOneScenario.Once(() => new PartOfAScenario("first"));
      readonly PartOfAScenarioThatFailsToDispose _second = ISharesOneScenario.Once(() => new PartOfAScenarioThatFailsToDispose("second"));
      readonly PartOfAScenario _third = ISharesOneScenario.Once(() => new PartOfAScenario("third"));

      [XF] public void a_fact() => XunitRunOfAClass.Record($"a fact looked at {_first}, {_second} and {_third}");
   }

   [XF] public async Task the_parts_built_before_and_after_it_are_disposed_still() =>
      (await _run).Recorded.Where(it => it.EndsWith(" disposed", StringComparison.Ordinal))
                  .Must().SequenceEqual(["third disposed", "first disposed"]);

   [XF] public async Task xUnit_reports_that_the_class_failed_to_clean_up_with_its_exception() =>
      (await _run).FailuresToCleanUpTheClass.Must().Satisfy(it => it.Count == 1 && it[0].Contains("second could not be disposed.", StringComparison.Ordinal));

   [XF] public async Task the_fact_passes() =>
      (await _run).Summary.Failed.Must().Be(0);
}
