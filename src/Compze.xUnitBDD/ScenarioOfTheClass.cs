using System.Collections.Concurrent;
using Compze.Contracts;
using Compze.xUnitBDD._private;
using Xunit;
using Xunit.Sdk;

namespace Compze.xUnitBDD;

/// <summary>
/// The scenario that all the facts of a class that implements <see cref="ISharesOneScenario"/> look at: the parts that the<br/>
/// class's constructors build with <see cref="ISharesOneScenario.Once{TPart}"/>, each built as the first fact is set up<br/>
/// and kept for the others.
/// </summary>
/// <remarks>
/// xUnit makes one for each such class, as the class's fixture, as the class starts, and disposes it as the class finishes,<br/>
/// however many of its facts ran: then each part that is disposable is disposed, the last built first. For a part that is<br/>
/// a task, what it completes with is disposed, once it has.
/// </remarks>
public sealed class ScenarioOfTheClass : IAsyncDisposable
{
   readonly ConcurrentDictionary<PlaceInTheCode, Lazy<object?>> _partsByWhereTheyAreBuilt = new();
   readonly DisposalTracker _partsDisposedAsItEnds = new();

   /// <summary>For xUnit, which makes one for each class that implements <see cref="ISharesOneScenario"/>, as the class starts.</summary>
   public ScenarioOfTheClass() {}

   /// <summary>Ends the scenario as xUnit finishes the class: disposes each part that is disposable, the last built first.</summary>
   /// <remarks>Each part is disposed even when one disposed before it fails: then this throws, once all the parts are disposed.</remarks>
   public ValueTask DisposeAsync() => _partsDisposedAsItEnds.DisposeAsync();

   /// <summary>The scenario of the class whose fact runs now, which xUnit made as the class started, as its fixture.</summary>
   internal static ScenarioOfTheClass OfTheClassThatRunsNow()
   {
      var scenario = TestContext.Current.FixtureOfTheClass<ScenarioOfTheClass>();
      Contract.State.Assert(scenario is not null,
                            () => $"{TestContext.Current.TestClass?.TestClassName ?? "Code that runs in no class of tests"} calls {nameof(ISharesOneScenario)}.{nameof(ISharesOneScenario.Once)}, " +
                                  $"so it must implement {nameof(ISharesOneScenario)}: only the facts of such a class share a scenario, which xUnit ends as the class finishes.");
      return scenario;
   }

   /// <summary>The part that <paramref name="built"/> builds at <paramref name="whereItIsBuilt"/>: built by the first call, and the same for each call after it.</summary>
   internal TPart Part<TPart>(PlaceInTheCode whereItIsBuilt, Func<TPart> built) =>
      (TPart)_partsByWhereTheyAreBuilt.GetOrAdd(whereItIsBuilt, _ => new Lazy<object?>(() => Built(built), LazyThreadSafetyMode.ExecutionAndPublication))
                                      .Value!;

   object? Built<TPart>(Func<TPart> built)
   {
      var part = built();
      if(part is Task builtAsynchronously) _partsDisposedAsItEnds.AddAsyncAction(builtAsynchronously.DisposeWhatItCompletesWithAsync);
      else _partsDisposedAsItEnds.Add(part);
      return part;
   }
}
