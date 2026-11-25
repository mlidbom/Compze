using Compze.Must;
using Xunit.v3;

namespace Compze.xUnitBDD.Tests;

public class When_xUnit_discovers_an_XF
{
   public abstract class AnAbstractClass
   {
      [XF] public void a_fact_declared_on_an_abstract_class() => throw new InvalidOperationException("An [XF] declared on an abstract class must never run.");
   }

#pragma warning disable CA1812 // Never instantiated: it is internal so that xUnit's own discovery skips it, and only these specifications discover it.
   internal sealed class DerivedFromAnAbstractClass : AnAbstractClass;
#pragma warning restore CA1812

   [XF] public async Task declared_in_the_class_it_is_one_test_of_the_class() =>
      (await XunitDiscovery.TestCases(typeof(AfterSomethingHappened), nameof(AfterSomethingHappened.ThisIsTrue)))
        .Must().HaveCount(1)
        .Satisfy(it => it.Single() is not ExecutionErrorTestCase);

   //A static class is abstract to .NET's reflection, and xUnit runs it.
   public static class AStaticClass
   {
      [XF] public static void a_fact_declared_in_a_static_class() {}
   }

   [XF] public async Task declared_in_a_static_class_it_is_one_test_of_the_class() =>
      (await XunitDiscovery.TestCases(typeof(AStaticClass), nameof(AStaticClass.a_fact_declared_in_a_static_class)))
        .Must().HaveCount(1)
        .Satisfy(it => it.Single() is not ExecutionErrorTestCase);

   [XF] public async Task inherited_from_a_class_that_is_not_abstract_it_is_no_test_of_the_derived_class() =>
      (await XunitDiscovery.TestCases(typeof(AfterSomethingHappened.AndThenThisHappened), nameof(AfterSomethingHappened.ThisIsTrue)))
        .Must().BeEmpty();

   [XF] public async Task inherited_from_an_abstract_class_it_is_a_test_of_the_derived_class_that_fails_saying_to_make_it_a_Fact()
   {
      var testCases = await XunitDiscovery.TestCases(typeof(DerivedFromAnAbstractClass), nameof(AnAbstractClass.a_fact_declared_on_an_abstract_class));

      var errorMessage = testCases.Must().HaveCount(1).Actual.Single().Must().BeExactType<ExecutionErrorTestCase>().Actual.ErrorMessage;
      errorMessage.Must().Contain(typeof(AnAbstractClass).ToString())
                  .Contain("make it a [Fact]");
   }
}
