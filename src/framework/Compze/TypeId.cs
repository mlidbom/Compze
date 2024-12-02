using System;
using Compze.Contracts;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Compze;

class TypeId
{
   public Guid GuidValue { get; private set; }

   // ReSharper disable once ImpureMethodCallOnReadonlyValueField
   public override string ToString() => GuidValue.ToString();

   public TypeId(Guid guidValue)
   {
      Assert.Argument.Assert(guidValue != Guid.Empty);
      GuidValue = guidValue;
   }

   public override bool Equals(object? other) => other is TypeId otherTypeId && otherTypeId.GuidValue.Equals(GuidValue);
   // ReSharper disable once NonReadonlyMemberInGetHashCode
   public override int GetHashCode() => GuidValue.GetHashCode();

   public static bool operator ==(TypeId left, TypeId right) => Equals(left, right);
   public static bool operator !=(TypeId left, TypeId right) => !Equals(left, right);
}