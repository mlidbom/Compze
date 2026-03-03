
// ReSharper disable UnusedParameter.Global

namespace Compze.Unit;

///<summary>The functional programming unit concept.
/// Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>.
/// Simply return Unit.Value from methods with no return value instead of declaring them as void,
/// or use <see cref="UnitConverterExtensions.From"/> to avoid that pesky extra line:
/// <code>
///   public Unit DoSomething() => Unit.From(() =>
///   {
///      //Do something here
///   });
/// </code>
/// We personally prefer using a global using alias that maps this to the lowercase name:
/// <c>global using unit = Compze.Unit;</c>
/// </summary>
#pragma warning disable CA1724 //Same name as the namespace
public readonly struct Unit : IEquatable<Unit>
{
   public static readonly Unit Value = default;

   public override string ToString() => "()";

   public bool Equals(Unit _) => true;
   public override bool Equals(object? obj) => obj is Unit;

   public static bool operator ==(Unit _, Unit __) => true;
   public static bool operator !=(Unit _, Unit __) => false;

   public override int GetHashCode() => 392576489;
}
