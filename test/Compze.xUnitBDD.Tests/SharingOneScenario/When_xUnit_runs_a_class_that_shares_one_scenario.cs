using Compze.Must;
using Xunit;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_xUnit_runs_a_class_that_shares_one_scenario : ISharesOneScenario
{
   const string AFactLookedAt = "a fact looked at";

   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassThatSharesOneScenario)));

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassThatSharesOneScenario : ISharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _first = ISharesOneScenario.Once(() => new PartOfAScenario("first"));
      readonly PartOfAScenarioDisposedAsynchronously _second = ISharesOneScenario.Once(() => new PartOfAScenarioDisposedAsynchronously("second"));
      readonly PartOfAScenario _third = ISharesOneScenario.Once(() => new PartOfAScenario("third")), _fourth = ISharesOneScenario.Once(() => new PartOfAScenario("fourth"));

      [XF] public void a_fact() => LookAtTheParts();
      [XF] public void another_fact() => LookAtTheParts();
      [Theory, InlineData(1), InlineData(2)] public void a_theory(int _) => LookAtTheParts();

      void LookAtTheParts() => XunitRunOfAClass.Record($"{AFactLookedAt} {_first}, {_second}, {_third} and {_fourth}");
   }

   [XF] public async Task its_two_facts_and_the_two_data_rows_of_its_theory_pass() =>
      (await _run).Summary.Must().Satisfy(it => it is { Total: 4, Failed: 0 });

   [XF] public async Task each_part_is_built_once_for_all_its_facts() =>
      (await _run).Recorded.Where(it => it.EndsWith(" built", StringComparison.Ordinal))
                  .Must().SequenceEqual(["first built", "second built", "third built", "fourth built"]);

   [XF] public async Task two_parts_built_on_one_line_are_two_parts() =>
      (await _run).Recorded.Must().Contain("third built").Contain("fourth built");

   [XF] public async Task each_fact_looks_at_the_same_parts() =>
      (await _run).Recorded.Where(it => it.StartsWith(AFactLookedAt, StringComparison.Ordinal)).Distinct()
                  .Must().HaveCount(1);

   [XF] public async Task no_part_is_disposed_until_all_its_facts_are_done() =>
      (await _run).Recorded.TakeWhile(it => !it.Contains(" disposed", StringComparison.Ordinal))
                  .Where(it => it.StartsWith(AFactLookedAt, StringComparison.Ordinal))
                  .Must().HaveCount(4);

   [XF] public async Task each_part_is_disposed_as_the_class_finishes_the_last_built_first_and_asynchronously_when_it_is_only_asynchronously_disposable() =>
      (await _run).Recorded.Where(it => it.Contains(" disposed", StringComparison.Ordinal))
                  .Must().SequenceEqual(["fourth disposed", "third disposed", "second disposed asynchronously", "first disposed"]);
}
