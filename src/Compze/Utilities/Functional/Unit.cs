using System;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System;
#pragma warning restore IDE0130

///<summary>The functional programming unit concept.
/// Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>.
/// Named unit, lowercase, in the hope that one day a language keyword will appear and this will ease migration.
/// Simply return unit.Value instead of void from methods with no return value,
/// or use <see cref="From"/> to avoid that pesky extra line:
/// <code>
///   public unit DoSomething() => unit.From(() =>
///   {
///      //Do something here
///   });
/// </code>
/// </summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
// ReSharper disable once InconsistentNaming
public readonly struct unit : IEquatable<unit>
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
{
   // ReSharper disable once MemberCanBeInternal
   public static readonly unit Value = default;

   ///<summary>Executes the task and returns unit making for easily returning unit without extra lines: unit Method() => unit.From(() => DoSomething())</summary>
   public static unit From(Action action)
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
