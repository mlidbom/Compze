using System;
using System.Threading.Tasks;

namespace Composable.SystemCE;

///<summary>Enables harmonizing on only using methods with return values without losing the semantic information that their return value is meaningless. Return <see cref="VoidCE.Instance"/> from methods with "no" return value. </summary>
struct VoidCE : IEquatable<VoidCE>, IComparable<VoidCE>, IStaticInstancePropertySingleton
{
   public static readonly VoidCE Instance = new();
   public static readonly Task<VoidCE> InstanceTask = Task.FromResult(Instance);

   public static VoidCE From(Action action)
   {
      action();
      return Instance;
   }

   public override string ToString() => "()";

   public bool Equals(VoidCE _) => true;
   public override bool Equals(object? other) => other is VoidCE;
   public static bool operator ==(VoidCE _, VoidCE __) => true;
   public static bool operator !=(VoidCE _, VoidCE __) => false;
   public int CompareTo(VoidCE _) => 0;

   public override int GetHashCode() => 392576489;
}
