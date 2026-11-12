using System.Reflection;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Compze.Must.Specifications;

/// <summary>
/// Failing assertions on values whose serialization once killed the test process, or hung it. Each is explicit, so no
/// ordinary test run runs it: <see cref="When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process"/> runs
/// each through <see cref="ExplicitTestInAChildProcess"/>, so that a regression fails a specification instead of killing the run.
/// </summary>
public class ScenariosThatOnceKilledTheTestProcess
{
   static string MessageOf(Action failingAssertion) => Invoking(failingAssertion).Must().Throw<AssertionFailedException>().Which.Message;

   [XF(Explicit = true)] public void a_value_holding_a_delegate() =>
      MessageOf(() => new HoldsADelegate().Must().Satisfy(_ => false)).Must().Contain("(delegate System.Func<System.Int32> calling ");

   [XF(Explicit = true)] public void a_value_holding_a_method() =>
      MessageOf(() => new HoldsAMethod().Must().Satisfy(_ => false)).Must().Contain($"(Method {typeof(object).FullName}.{nameof(ToString)})");

   [XF(Explicit = true)] public void a_value_holding_a_task_that_never_completes() =>
      MessageOf(() => new HoldsATaskThatNeverCompletes().Must().Satisfy(_ => false)).Must().Contain("\"Status\": \"WaitingForActivation\"");

   [XF(Explicit = true)] public void a_value_nested_deeper_than_the_stack_allows() =>
      MessageOf(() => Link.ChainOfLength(100_000).Must().Satisfy(_ => false)).Must().Contain("The object graph nests deeper than this thread's stack allows");

   [XF(Explicit = true)] public void a_value_with_a_getter_that_throws_800_levels_down() =>
      MessageOf(() => Link.ChainOfLength(800, theLastOneThrows: true).Must().Satisfy(_ => false)).Must().Contain("thrown 800 levels down");

   class HoldsADelegate
   {
      public Func<int> Callback { get; } = () => 42;
   }

   class HoldsAMethod
   {
      public MethodInfo Method { get; } = typeof(object).GetMethod(nameof(ToString))!;
   }

   class HoldsATaskThatNeverCompletes
   {
      public Task<int> Task { get; } = new TaskCompletionSource<int>().Task;
   }

   class Link
   {
      readonly bool _throws;

      Link(bool throws) => _throws = throws;

      public Link? Next { get; private set; }

      public string Value => _throws ? throw new InvalidOperationException("thrown 800 levels down") : "value";

      public static Link ChainOfLength(int length, bool theLastOneThrows = false)
      {
         var first = new Link(throws: theLastOneThrows && length == 1);
         var last = first;
         for(var position = 2; position <= length; position++)
         {
            last = last.Next = new Link(throws: theLastOneThrows && position == length);
         }

         return first;
      }
   }
}
