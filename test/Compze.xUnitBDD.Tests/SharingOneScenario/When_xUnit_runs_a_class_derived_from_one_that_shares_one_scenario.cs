using Compze.Must;

namespace Compze.xUnitBDD.Tests.SharingOneScenario;

public class When_xUnit_runs_a_class_derived_from_one_that_shares_one_scenario : ISharesOneScenario
{
   const string AFactLookedAt = "a fact looked at";

   readonly Task<XunitRunOfAClass> _run = ISharesOneScenario.Once(() => XunitRunOfAClass.Of(typeof(AClassDerivedFromOneThatSharesOneScenario)));

   internal abstract class AClassThatSharesOneScenario : ISharesOneScenario
   {
      protected readonly PartOfAScenario PartTheBaseClassBuilds = ISharesOneScenario.Once(() => new PartOfAScenario("the base class's part"));
   }

#pragma warning disable CA1812, xUnit1000 // Internal, so that xUnit's own discovery skips it: only XunitRunOfAClass has xUnit run it, for these specifications.
   internal sealed class AClassDerivedFromOneThatSharesOneScenario : AClassThatSharesOneScenario
#pragma warning restore CA1812, xUnit1000
   {
      readonly PartOfAScenario _itsOwnPart;

      public AClassDerivedFromOneThatSharesOneScenario() => _itsOwnPart = ISharesOneScenario.Once(() => new PartOfAScenario("its own part"));

      [XF] public void a_fact() => XunitRunOfAClass.Record($"{AFactLookedAt} {PartTheBaseClassBuilds} and {_itsOwnPart}");
      [XF] public void another_fact() => XunitRunOfAClass.Record($"{AFactLookedAt} {PartTheBaseClassBuilds} and {_itsOwnPart}");
   }

   [XF] public async Task it_builds_the_part_its_base_class_builds_and_then_its_own_once_each_for_a_scenario_of_its_own() =>
      (await _run).Recorded.Where(it => it.EndsWith(" built", StringComparison.Ordinal))
                  .Must().SequenceEqual(["the base class's part built", "its own part built"]);

   [XF] public async Task each_of_its_facts_looks_at_the_same_parts() =>
      (await _run).Recorded.Where(it => it.StartsWith(AFactLookedAt, StringComparison.Ordinal))
                  .Must().Satisfy(it => it.Count() == 2 && it.Distinct().Count() == 1);

   [XF] public async Task its_scenario_ends_as_it_finishes_its_own_part_first() =>
      (await _run).Recorded.Where(it => it.EndsWith(" disposed", StringComparison.Ordinal))
                  .Must().SequenceEqual(["its own part disposed", "the base class's part disposed"]);
}
