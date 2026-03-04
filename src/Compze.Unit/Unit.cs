using System.Diagnostics.Contracts;

// ReSharper disable UnusedParameter.Global

namespace Compze.Unit;

///<summary>The functional programming unit concept.
/// Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>.
/// Simply return Unit.Value from methods with no return value instead of declaring them as void,
/// or use <see cref="Invoke"/> to avoid that pesky extra line:
/// <code>
///   public Unit DoSomething() => Unit.Invoke(() =>
///   {
///      //Do something here
///   });
/// </code>
/// </summary>
#pragma warning disable CA1724 //Same name as the namespace
[Serializable]
public readonly partial struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
   public static readonly Unit Value = default;

   [Pure] public override string ToString() => "()";

   [Pure] public bool Equals(Unit _) => true;
   [Pure] public override bool Equals(object? obj) => obj is Unit;

   [Pure] public static bool operator ==(Unit _, Unit __) => true;
   [Pure] public static bool operator !=(Unit _, Unit __) => false;

   [Pure] public override int GetHashCode() => 0;

   [Pure] public int CompareTo(Unit _) => 0;

   [Pure] public int CompareTo(object? obj) => obj switch
   {
      null => 1,
      Unit => 0,
      _    => throw new ArgumentException($"Object must be of type {nameof(Unit)}")
   };

   [Pure] public static implicit operator Unit(ValueTuple _) => Value;
   [Pure] public static implicit operator ValueTuple(Unit _) => default;
}
