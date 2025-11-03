using System;
using System.Collections;

namespace Compze.Utilities.Tests.Testing.Fluent;

class ComparableWithErrorInjectionSupport : IEquatable<ComparableWithErrorInjectionSupport>, 
                                             IComparable<ComparableWithErrorInjectionSupport>, 
                                             IComparable,
                                             IStructuralEquatable,
                                             IStructuralComparable
{
   readonly int _value;
   readonly BreakComparableMethod _breakComparableMethod;

   public ComparableWithErrorInjectionSupport(int value, BreakComparableMethod breakComparableMethod = BreakComparableMethod.None)
   {
      _value = value;
      _breakComparableMethod = breakComparableMethod;
   }

   public override bool Equals(object? obj)
   {
      if(obj is not ComparableWithErrorInjectionSupport other) return false;
      var result = _value == other._value;
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.ObjectEquals)) result = !result;
      return result;
   }

   public bool Equals(ComparableWithErrorInjectionSupport? other)
   {
      if(other is null) return false;
      var result = _value == other._value;
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.IEquatable)) result = !result;
      return result;
   }

   public override int GetHashCode()
   {
      var hash = _value.GetHashCode();
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.GetHashCode)) hash = ~hash;
      return hash;
   }

   public int CompareTo(ComparableWithErrorInjectionSupport? other)
   {
      if(other is null) return 1;
      var result = _value.CompareTo(other._value);
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.IComparableGeneric) && result == 0) result = 1;
      return result;
   }

   public int CompareTo(object? obj)
   {
      if(obj is not ComparableWithErrorInjectionSupport other) throw new ArgumentException("Object is not a ComparableWithErrorInjectionSupport");
      var result = _value.CompareTo(other._value);
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.IComparable) && result == 0) result = 1;
      return result;
   }

   public static bool operator ==(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
   {
      if(ReferenceEquals(left, right)) return true;
      if(left is null || right is null) return false;
      var result = left._value == right._value;
      if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorEquality)) result = !result;
      return result;
   }

   public static bool operator !=(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
   {
      if(ReferenceEquals(left, right)) return false;
      if(left is null || right is null) return true;
      var result = left._value != right._value;
      if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorInequality)) result = !result;
      return result;
   }

   public static bool operator <(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
   {
      if(left is null || right is null) return false;
      var result = left._value < right._value;
      if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorLessThan)) result = !result;
      return result;
   }

   public static bool operator >(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
   {
      if(left is null || right is null) return false;
      var result = left._value > right._value;
      if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorGreaterThan)) result = !result;
      return result;
   }

   public static bool operator <=(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
   {
      if(left is null || right is null) return false;
      var result = left._value <= right._value;
      if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorLessThanOrEqual)) result = !result;
      return result;
   }

   public static bool operator >=(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
   {
      if(left is null || right is null) return false;
      var result = left._value >= right._value;
      if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorGreaterThanOrEqual)) result = !result;
      return result;
   }

   // IStructuralEquatable implementation
   bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
   {
      if(other is not ComparableWithErrorInjectionSupport otherValue) return false;
      var result = _value == otherValue._value;
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.IStructuralEquatable)) result = !result;
      return result;
   }

   int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
   {
      var hash = comparer.GetHashCode(_value);
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.IStructuralEquatable)) hash = ~hash;
      return hash;
   }

   // IStructuralComparable implementation
   int IStructuralComparable.CompareTo(object? other, IComparer comparer)
   {
      if(other is not ComparableWithErrorInjectionSupport otherValue) throw new ArgumentException("Object is not a ComparableWithErrorInjectionSupport");
      var result = _value.CompareTo(otherValue._value);
      if(_breakComparableMethod.HasFlag(BreakComparableMethod.IStructuralComparable) && result == 0) result = 1;
      return result;
   }
}
