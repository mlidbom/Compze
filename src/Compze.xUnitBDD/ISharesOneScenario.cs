using System.Runtime.CompilerServices;
using Compze.Contracts;
using Compze.xUnitBDD._private;
using Xunit;

namespace Compze.xUnitBDD;

/// <summary>
/// Marks a class of specifications whose facts all look at one scenario, built once for all of them: each part that its<br/>
/// constructors build with <see cref="Once{TPart}"/> is built as its first fact is set up, and kept for the facts after it.<br/>
/// The scenario ends as xUnit finishes the class, however many of its facts ran.
/// </summary>
/// <remarks>
/// xUnit makes a new instance of a class for each of its facts, so what its constructors set up is set up again for each<br/>
/// fact. For a scenario that takes seconds to build, such as one that starts a server, that soon adds up to minutes.<br/>
/// Implementing this interface promises that the class's facts change nothing in the scenario: a fact that changes it<br/>
/// changes it for the facts that run after it.
/// </remarks>
/// <remarks>
/// Each class that implements it has a scenario of its own. A nested class that derives from one builds the parts its base<br/>
/// class builds again, for its own facts, and then its own parts on top of them.<br/>
/// What the facts look at must all be built with <see cref="Once{TPart}"/>: what a constructor builds otherwise is built<br/>
/// again for each fact, and a part built from it keeps what the first fact's constructor built.<br/>
/// Not for a class whose facts run under different settings that its constructors read, such as the combinations of<br/>
/// Compze.xUnitMatrix: the scenario built under the first would be the one the others look at.<br/>
/// It is an xUnit class fixture (<see cref="IClassFixture{TFixture}"/>): xUnit makes a <see cref="ScenarioOfTheClass"/> for<br/>
/// each class that implements it as the class starts, and disposes it as the class finishes.
/// </remarks>
public interface ISharesOneScenario : IClassFixture<ScenarioOfTheClass>
{
   /// <summary>
   /// The part of the scenario that <paramref name="built"/> builds: built as the class's first fact is set up, and the same<br/>
   /// part for each fact after it. Called in the constructors of a class that implements <see cref="ISharesOneScenario"/>.
   /// </summary>
   /// <remarks>
   /// Each call in the code builds a part of its own, kept by the call's file and line, which the compiler fills in as<br/>
   /// <paramref name="file"/> and <paramref name="line"/>, and by the lambda it is given. A method that calls this one for<br/>
   /// its callers, such as a base class's for its derived classes, passes on its own caller's file and line.<br/>
   /// A part that throws as it is built is not built again: each fact that asks for it fails with its exception.<br/>
   /// When the class's facts run in parallel, one of them builds the part, and the others wait for it.<br/>
   /// A part that is disposable is disposed as the scenario ends: see <see cref="ScenarioOfTheClass"/>.
   /// </remarks>
   static TPart Once<TPart>(Func<TPart> built, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
   {
      Contract.Argument.NotNull(built);
      return ScenarioOfTheClass.OfTheClassThatRunsNow().Part(new PlaceInTheCode(file, line, built.Method), built);
   }
}
