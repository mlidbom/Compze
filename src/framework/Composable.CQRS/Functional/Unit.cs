using System;
using System.Threading.Tasks;
using Composable.SystemCE;

namespace Composable.Functional;

///<summary>The functional programming unit concept. Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>. Simply return <see cref="Unit"/> instead of void from methods with no return value.</summary>
public struct Unit : IEquatable<Unit>, IComparable<Unit>, IStaticInstancePropertySingleton
{
   public static readonly Unit Instance = new();
   public static readonly Task<Unit> InstanceTask = Task.FromResult(Instance);

   public static Unit From(Action action)
   {
      action();
      return Instance;
   }

   public readonly override string ToString() => "()";

   public readonly bool Equals(Unit _) => true;
   public readonly override bool Equals(object? other) => other is Unit;
   public static bool operator ==(Unit _, Unit __) => true;
   public static bool operator !=(Unit _, Unit __) => false;
   public readonly int CompareTo(Unit _) => 0;

   public readonly override int GetHashCode() => 392576489;
}
