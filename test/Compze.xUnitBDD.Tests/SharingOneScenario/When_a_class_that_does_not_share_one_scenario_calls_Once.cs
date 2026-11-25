using Compze.Must;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_a_class_that_does_not_share_one_scenario_calls_Once : ISharesOneScenario
{
   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassThatDoesNotShareOneScenario)));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassThatDoesNotShareOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _part = ISharesOneScenario.Once(() => new PartOfAScenario("the part"));

      [XF] public void a_fact() => XunitRunOfAClass.Record($"a fact looked at {_part}");
   }

   [XF] public async Task its_fact_fails_saying_that_the_class_must_implement_ISharesOneScenario() =>
      (await _run).FailuresOfTheFacts.Must().Satisfy(it => it.Count == 1 && it[0].Contains($"so it must implement {nameof(ISharesOneScenario)}", StringComparison.Ordinal));

   [XF] public async Task no_part_is_built() =>
      (await _run).Recorded.Must().BeEmpty();
}
