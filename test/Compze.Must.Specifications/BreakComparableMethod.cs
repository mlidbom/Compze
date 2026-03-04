

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

[Flags]
enum BreakComparableMethod
{
   None = 0,
   ObjectEquals = 1 << 0,              // 1
   IEquatable = 1 << 1,                // 2
   OperatorEquality = 1 << 2,          // 4
   OperatorInequality = 1 << 3,        // 8
   IComparableGeneric = 1 << 4,        // 16
   IComparable = 1 << 5,               // 32
   OperatorLessThan = 1 << 6,          // 64
   OperatorLessThanOrEqual = 1 << 7,   // 128
   OperatorGreaterThan = 1 << 8,       // 256
   OperatorGreaterThanOrEqual = 1 << 9, // 512
   GetHashCode = 1 << 10,              // 1024
   IStructuralEquatable = 1 << 11,     // 2048
   IStructuralComparable = 1 << 12     // 4096
}
