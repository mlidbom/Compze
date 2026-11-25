using Compze.Must;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_a_part_of_a_shared_scenario_is_a_task : ISharesOneScenario
{
   const string WhyThePartFailed = "The part could not be built.";

   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassWhosePartsAreTasks)));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassWhosePartsAreTasks : ISharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly Task<PartOfAScenario> _thePartItsFactsAwait = ISharesOneScenario.Once(() => BuiltAsynchronously("the part its facts await", TimeSpan.Zero));
      readonly Task<PartOfAScenario> _thePartNoFactAwaits = ISharesOneScenario.Once(() => BuiltAsynchronously("the part no fact awaits", TimeSpan.FromMilliseconds(300)));
      readonly Task<PartOfAScenario> _thePartThatFails = ISharesOneScenario.Once(FailsAsynchronously);

      [XF] public async Task a_fact() => XunitRunOfAClass.Record($"a fact looked at {await _thePartItsFactsAwait}");
      [XF] public async Task another_fact() => XunitRunOfAClass.Record($"a fact looked at {await _thePartItsFactsAwait}");
      [XF] public async Task a_fact_that_awaits_the_part_that_fails() => XunitRunOfAClass.Record($"a fact looked at {await _thePartThatFails}");
      [XF] public void a_fact_that_looks_at_whether_the_part_no_fact_awaits_has_completed() => XunitRunOfAClass.Record($"the part no fact awaits has completed: {_thePartNoFactAwaits.IsCompleted}");

      static async Task<PartOfAScenario> BuiltAsynchronously(string name, TimeSpan after)
      {
         await Task.Yield();
         await Task.Delay(after);
         return new PartOfAScenario(name);
      }

      static async Task<PartOfAScenario> FailsAsynchronously()
      {
         await Task.Yield();
         throw new InvalidOperationException(WhyThePartFailed);
      }
   }

   [XF] public async Task the_part_no_fact_awaits_is_still_being_built_as_the_facts_run() =>
      (await _run).Recorded.Must().Contain("the part no fact awaits has completed: False");

   [XF] public async Task what_each_task_completes_with_is_disposed_as_the_class_finishes_also_when_no_fact_awaited_it() =>
      (await _run).Recorded.Where(it => it.EndsWith(" disposed", StringComparison.Ordinal))
                  .Must().SequenceEqual(["the part no fact awaits disposed", "the part its facts await disposed"]);

   [XF] public async Task a_task_that_fails_fails_the_fact_that_awaits_it() =>
      (await _run).FailuresOfTheFacts.Must().SequenceEqual([WhyThePartFailed]);

   [XF] public async Task the_class_cleans_up_without_failing() =>
      (await _run).FailuresToCleanUpTheClass.Must().BeEmpty();
}
