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
#pragma warning disable CA1036 // All Unit values are equal — ordering operators would be trivially true/false
#pragma warning disable CA2225 // Implicit conversions to/from ValueTuple are sufficient; named alternatives add no value for a type with a single possible value
[Serializable]
public readonly partial struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
   ///<summary>The single value of the <see cref="Unit"/> type.</summary>
   public static readonly Unit Value = default;

   ///<summary>Always returns <c>"()"</c>.</summary>
   [Pure] public override string ToString() => "()";

   ///<summary>Always returns <see langword="true"/>. All <see cref="Unit"/> values are equal.</summary>
   [Pure] public bool Equals(Unit _) => true;

   ///<summary>Returns <see langword="true"/> if <paramref name="obj"/> is a <see cref="Unit"/>; otherwise <see langword="false"/>.</summary>
   [Pure] public override bool Equals(object? obj) => obj is Unit;

   ///<summary>Always returns <see langword="true"/>. All <see cref="Unit"/> values are equal.</summary>
   [Pure] public static bool operator ==(Unit _, Unit __) => true;

   ///<summary>Always returns <see langword="false"/>. All <see cref="Unit"/> values are equal.</summary>
   [Pure] public static bool operator !=(Unit _, Unit __) => false;

   ///<summary>Always returns <c>0</c>.</summary>
   [Pure] public override int GetHashCode() => 0;

   ///<summary>Always returns <c>0</c>. All <see cref="Unit"/> values are equal.</summary>
   [Pure] public int CompareTo(Unit _) => 0;

   ///<summary>Returns <c>0</c> if <paramref name="obj"/> is a <see cref="Unit"/>; <c>1</c> if <see langword="null"/>; otherwise throws.</summary>
   [Pure] public int CompareTo(object? obj) => obj switch
   {
      null => 1,
      Unit => 0,
      _    => throw new ArgumentException($"Object must be of type {nameof(Unit)}")
   };

   ///<summary>Converts a <see cref="ValueTuple"/> to <see cref="Unit"/>.</summary>
   [Pure] public static implicit operator Unit(ValueTuple _) => Value;

   ///<summary>Converts a <see cref="Unit"/> to <see cref="ValueTuple"/>.</summary>
   [Pure] public static implicit operator ValueTuple(Unit _) => default;
}
