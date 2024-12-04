using System;
using System.Threading.Tasks;
using Compze.SystemCE;

namespace Compze.Functional;

///<summary>The functional programming unit concept. Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>. Simply return <see cref="Unit"/> instead of void from methods with no return value.</summary>
struct Unit : IEquatable<Unit>, IStaticInstancePropertySingleton
{
   internal static readonly Unit Instance = new();
   internal static readonly Task<Unit> InstanceTask = Task.FromResult(Instance);

   internal static Unit From(Action action)
   {
      action();
      return Instance;
   }

   internal static Unit Ignore<TValue>(TValue _) => Instance;

   public readonly override string ToString() => "()";

   public readonly bool Equals(Unit _) => true;
   public readonly override bool Equals(object? obj) => obj is Unit;
   public static bool operator ==(Unit _, Unit __) => true;
   public static bool operator !=(Unit _, Unit __) => false;

   public readonly override int GetHashCode() => 392576489;
}
