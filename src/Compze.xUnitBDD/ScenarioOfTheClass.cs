using System.Collections.Concurrent;
using Compze.Contracts;
using Compze.xUnitBDD._private;
using Xunit;
using Xunit.v3;

namespace Compze.xUnitBDD;

/// <summary>
/// The scenario that all the facts of a class that implements <see cref="ISharesOneScenario"/> look at: the parts that the<br/>
/// class's constructors build with <see cref="ISharesOneScenario.Once{TPart}"/>, each built as the first fact is set up<br/>
/// and kept for the others.
/// </summary>
/// <remarks>
/// xUnit makes one for each such class, as the class's fixture, as the class starts, and disposes it as the class finishes,<br/>
/// however many of its facts ran: then each part that is disposable is disposed, the last built first.
/// </remarks>
public sealed class ScenarioOfTheClass : IAsyncDisposable
{
   //review-naming: ScenarioOfTheClass, which a consumer meets only as what ISharesOneScenario is a class fixture of.
   readonly ConcurrentDictionary<PlaceInTheCode, Lazy<object?>> _partsByWhereTheyAreBuilt = new();
   readonly ConcurrentStack<object> _partsToDisposeTheLastBuiltOnTop = new();

   /// <summary>For xUnit, which makes one for each class that implements <see cref="ISharesOneScenario"/>, as the class starts.</summary>
   public ScenarioOfTheClass() {}

   /// <summary>Ends the scenario as xUnit finishes the class: disposes each part that is disposable, the last built first.</summary>
   /// <remarks>Each part is disposed even when one disposed before it fails: then this throws, once all the parts are disposed.</remarks>
   public async ValueTask DisposeAsync()
   {
      var failures = ExceptionAggregator.Create();
      while(_partsToDisposeTheLastBuiltOnTop.TryPop(out var part))
         await failures.RunAsync(() => DisposeOfAsync(part)).ConfigureAwait(false);
      failures.ThrowIfFaulted();

      static async ValueTask DisposeOfAsync(object part)
      {
         if(part is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync().ConfigureAwait(false);
         else ((IDisposable)part).Dispose();
      }
   }

   /// <summary>The scenario of the class whose fact runs now, which xUnit made as the class started, as its fixture.</summary>
   internal static ScenarioOfTheClass OfTheClassThatRunsNow()
   {
      var lookup = TestContext.Current.GetFixture(typeof(ScenarioOfTheClass));
      if(!lookup.IsCompleted)
         throw new InvalidOperationException($"xUnit didn't hand out the {nameof(ScenarioOfTheClass)} of a class at once, as it does up to version 4.0.3, which Compze.xUnitBDD is made for.");
#pragma warning disable VSTHRD002 // No wait: the lookup has completed, as the check above makes sure.
      var scenario = lookup.Result as ScenarioOfTheClass;
#pragma warning restore VSTHRD002
      Contract.State.Assert(scenario is not null,
                            () => $"{TestContext.Current.TestClass?.TestClassName ?? "Code that runs in no class of tests"} calls {nameof(ISharesOneScenario)}.{nameof(ISharesOneScenario.Once)}, " +
                                  $"so it must implement {nameof(ISharesOneScenario)}: only the facts of such a class share a scenario, which xUnit ends as the class finishes.");
      return scenario;
   }

   /// <summary>The part that <paramref name="built"/> builds at <paramref name="line"/> of <paramref name="file"/>: built by the first call, and the same for each call after it.</summary>
   internal TPart Part<TPart>(string file, int line, Func<TPart> built) =>
      (TPart)_partsByWhereTheyAreBuilt.GetOrAdd(new PlaceInTheCode(file, line, built.Method),
                                                _ => new Lazy<object?>(() => Built(built), LazyThreadSafetyMode.ExecutionAndPublication))
                                      .Value!;

   object? Built<TPart>(Func<TPart> built)
   {
      var part = built();
      if(part is IAsyncDisposable or IDisposable) _partsToDisposeTheLastBuiltOnTop.Push(part);
      return part;
   }
}
