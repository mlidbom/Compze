#pragma warning disable IDE0130
namespace System;
#pragma warning restore IDE0130

///<summary>The functional programming unit concept.
/// Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>.
/// Simply return unit.Value instead of void from methods with no return value.
/// Placed in System and named "unit" in the hope that eventually unit will become a language feature and then
/// migration from this struct to the language feature might conceivably be no more than removing the reference to this package.
/// </summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
public readonly struct unit : IEquatable<unit>
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
{
   // ReSharper disable once MemberCanBeInternal
   public static readonly unit Value = default;

   ///<summary>Executes the task and returns unit making for easily returning unit without extra lines: unit Method() => unit.From(() => DoSomething())</summary>
   internal static unit From(Action action)
   {
      action();
      return Value;
   }

   public override string ToString() => "()";

   public bool Equals(unit _) => true;
   public override bool Equals(object? obj) => obj is unit;
   public static bool operator ==(unit _, unit __) => true;
   public static bool operator !=(unit _, unit __) => false;

   public override int GetHashCode() => 392576489;
}