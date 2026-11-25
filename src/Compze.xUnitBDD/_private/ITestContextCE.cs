using Xunit;

namespace Compze.xUnitBDD._private;

/// <summary>What Compze.xUnitBDD reads from xUnit's context of the test that runs now.</summary>
static class ITestContextCE
{
   extension(ITestContext @this)
   {
      /// <summary>
      /// The class fixture of type <typeparamref name="TFixture"/> that xUnit made for the class of the test that runs now, as<br/>
      /// the class started: null when the class has none of that type.
      /// </summary>
      /// <remarks>
      /// xUnit hands out a fixture it has already made at once, though its lookup is asynchronous. This fails, saying so, should<br/>
      /// a later xUnit not, rather than wait for it.
      /// </remarks>
      internal TFixture? FixtureOfTheClass<TFixture>() where TFixture : class
      {
         var lookup = @this.GetFixture(typeof(TFixture));
         //A hand-written check, not Contract.State.Assert: CA2012 takes no other as the guard that makes lookup.Result safe to read.
         if(!lookup.IsCompleted)
            throw new InvalidOperationException($"xUnit didn't hand out the class fixture {typeof(TFixture).Name} at once, as it does up to version 4.0.3, which Compze.xUnitBDD is made for.");
#pragma warning disable VSTHRD002 // No wait: the lookup has completed, as the check above makes sure.
         return lookup.Result as TFixture;
#pragma warning restore VSTHRD002
      }
   }
}
