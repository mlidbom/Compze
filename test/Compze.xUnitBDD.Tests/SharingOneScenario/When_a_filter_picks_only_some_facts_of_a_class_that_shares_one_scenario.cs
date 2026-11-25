using Compze.Must;
using Xunit.Sdk;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_a_filter_picks_only_some_facts_of_a_class_that_shares_one_scenario : ISharesOneScenario
{
   readonly Task<XunitRunOfAClass> _run =
      ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassThatSharesOneScenario), ParallelMode.None, [nameof(AClassThatSharesOneScenario.a_fact)]));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassThatSharesOneScenario : ISharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _part = ISharesOneScenario.Once(() => new PartOfAScenario("the part"));

      [XF] public void a_fact() => XunitRunOfAClass.Record($"a fact looked at {_part}");
      [XF] public void a_fact_the_filter_leaves_out() => XunitRunOfAClass.Record($"a fact looked at {_part}");
      [XF] public void another_fact_the_filter_leaves_out() => XunitRunOfAClass.Record($"a fact looked at {_part}");
   }

   [XF] public async Task only_the_fact_it_picked_runs() =>
      (await _run).Summary.Total.Must().Be(1);

   [XF] public async Task the_scenario_ends_as_the_class_finishes_all_the_same() =>
      (await _run).Recorded.Must().Satisfy(it => it is ["the part built", _, "the part disposed"]);
}
