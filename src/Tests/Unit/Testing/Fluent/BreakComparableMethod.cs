using System;

namespace Compze.Tests.Unit.Testing.Fluent;

[Flags]
enum BreakComparableMethod
{
   None = 0,
   ObjectEquals = 1 << 0,
   IEquatable = 1 << 2,
   OperatorEquality = 1 << 3,
   OperatorInequality = 1 << 4,
   EqualityComparer = 1 << 5,
   IComparableGeneric = 1 << 6,
   IComparable = 1 << 7,
   OperatorLessThan = 1 << 8,
   OperatorLessThanOrEqual = 1 << 9,
   OperatorGreaterThan = 1 << 10,
   OperatorGreaterThanOrEqual = 1 << 11,
   GetHashCode = 1 << 12
}
