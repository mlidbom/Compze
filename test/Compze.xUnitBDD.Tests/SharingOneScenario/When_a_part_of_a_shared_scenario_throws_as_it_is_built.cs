using Compze.Must;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_a_part_of_a_shared_scenario_throws_as_it_is_built : ISharesOneScenario
{
   const string TheBuildStarted = "the part's build started";
   const string WhyTheBuildFailed = "The part could not be built.";

   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassWhosePartThrowsAsItIsBuilt)));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassWhosePartThrowsAsItIsBuilt : ISharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _part = ISharesOneScenario.Once<PartOfAScenario>(() =>
      {
         XunitRunOfAClass.Record(TheBuildStarted);
         throw new InvalidOperationException(WhyTheBuildFailed);
      });

      [XF] public void a_fact() => XunitRunOfAClass.Record($"a fact looked at {_part}");
      [XF] public void another_fact() => XunitRunOfAClass.Record($"a fact looked at {_part}");
   }

   [XF] public async Task each_fact_of_the_class_fails_with_its_exception() =>
      (await _run).FailuresOfTheFacts.Must().SequenceEqual([WhyTheBuildFailed, WhyTheBuildFailed]);

   [XF] public async Task it_is_not_built_again_for_the_second_fact() =>
      (await _run).Recorded.Must().SequenceEqual([TheBuildStarted]);
}
