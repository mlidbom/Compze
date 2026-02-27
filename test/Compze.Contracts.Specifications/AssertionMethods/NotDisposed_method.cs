using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotDisposed_method : AssertionMethodsTest
{
   sealed class TestDisposable : IDisposable
   {
      public void Dispose() { }
   }

   static readonly TestDisposable Instance = new();

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
