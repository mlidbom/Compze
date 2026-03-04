using Compze.Must;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotDisposed_method : AssertionMethodsTest
{
   sealed class TestDisposable : IDisposable
   {
      public void Dispose() {}
   }

   static NotDisposed_method() => new TestDisposable().Dispose(); //Just to get to 100% test coverage

   static TestDisposable Instance => new();

   public class called_when_disposed : NotDisposed_method
   {
      [XF] public void throws_ObjectDisposedException() =>
         Invoking(() => Asserter.NotDisposed(true, Instance)).Must().Throw<ObjectDisposedException>();
   }

   public class called_when_not_disposed : NotDisposed_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotDisposed(false, Instance).Must().Be(Asserter);
   }
}
