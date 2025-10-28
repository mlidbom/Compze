using System;

namespace Compze.Core.Public.Infrastructure;

public abstract class ValueWrapper<TValue>(TValue primitiveValue) : IEquatable<ValueWrapper<TValue>>,
                                                                    ISingleUntypedPrimitiveValueWrapper
   where TValue : IEquatable<TValue>
{
   public TValue PrimitiveValue { get; } = primitiveValue;
   object ISingleUntypedPrimitiveValueWrapper.UntypedPrimitiveValue => PrimitiveValue;

   public bool Equals(ValueWrapper<TValue>? other) => other != null 
                                                   && other.PrimitiveValue.Equals(PrimitiveValue);

   public override bool Equals(object? other) => other != null
                                              && other.GetType() == GetType()
                                              && Equals((ValueWrapper<TValue>)other);

   public override int GetHashCode() => PrimitiveValue.GetHashCode();
   public override string? ToString() => PrimitiveValue.ToString();

   public static bool operator ==(ValueWrapper<TValue>? left, ValueWrapper<TValue>? right) => Equals(left, right);
   public static bool operator !=(ValueWrapper<TValue>? left, ValueWrapper<TValue>? right) => !Equals(left, right);
}
