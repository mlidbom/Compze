using System;

namespace Compze.Core.Public.Infrastructure;

#pragma warning disable CA1033 //We are using explicit interface implementation to hide this from the public interface
public abstract class ValueWrapper<TValue>(TValue primitiveValue) : IEquatable<ValueWrapper<TValue>>,
                                                                    ISingleUntypedPrimitiveValueWrapper
   where TValue : IEquatable<TValue>
{
   // ReSharper disable once MemberCanBePrivate.Global
   public TValue PrimitiveValue { get; } = primitiveValue;
   object ISingleUntypedPrimitiveValueWrapper.UntypedPrimitiveValue => PrimitiveValue;

   public bool Equals(ValueWrapper<TValue>? other) => other != null
                                                   && other.PrimitiveValue.Equals(PrimitiveValue);

   public override bool Equals(object? obj) => obj != null
                                            && obj.GetType() == GetType()
                                            && Equals((ValueWrapper<TValue>)obj);

   public override int GetHashCode() => PrimitiveValue.GetHashCode();
   public override string ToString() => PrimitiveValue.ToString() ?? "";

   public static bool operator ==(ValueWrapper<TValue>? left, ValueWrapper<TValue>? right) => Equals(left, right);
   public static bool operator !=(ValueWrapper<TValue>? left, ValueWrapper<TValue>? right) => !Equals(left, right);
}
