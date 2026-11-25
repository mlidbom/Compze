using Compze.Must;
using Xunit.Sdk;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_xUnit_runs_the_facts_of_a_class_that_shares_one_scenario_in_parallel : ISharesOneScenario
{
   const string AFactIsBeingSetUp = "a fact is being set up";
   const string AFactLookedAt = "a fact looked at";

   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassWhoseFactsRunInParallel), ParallelMode.All));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassWhoseFactsRunInParallel : ISharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _part;

      public AClassWhoseFactsRunInParallel()
      {
         XunitRunOfAClass.Record(AFactIsBeingSetUp);
         _part = ISharesOneScenario.Once(() =>
         {
            SpinWait.SpinUntil(() => XunitRunOfAClass.RecordedSoFar.Count(it => it == AFactIsBeingSetUp) == 3, TimeSpan.FromSeconds(30));
            return new PartOfAScenario("the part");
         });
      }

      [XF] public void a_fact() => LookAtThePart();
      [XF] public void another_fact() => LookAtThePart();
      [XF] public void a_third_fact() => LookAtThePart();

      void LookAtThePart() => XunitRunOfAClass.Record($"{AFactLookedAt} {_part}");
   }

   [XF] public async Task one_fact_builds_the_part_while_the_other_two_wait_for_it() =>
      (await _run).Recorded.Where(it => !it.StartsWith(AFactLookedAt, StringComparison.Ordinal))
                  .Must().SequenceEqual([AFactIsBeingSetUp, AFactIsBeingSetUp, AFactIsBeingSetUp, "the part built", "the part disposed"]);

   [XF] public async Task all_three_facts_look_at_that_part() =>
      (await _run).Recorded.Where(it => it.StartsWith(AFactLookedAt, StringComparison.Ordinal)).ToList()
                  .Must().Satisfy(it => it.Count == 3 && it.Distinct().Count() == 1);
}
