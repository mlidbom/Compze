namespace Compze.Core.Public.Infrastructure;

#pragma warning disable CA1033 //We are using explicit interface implementation to hide this from the public interface
public class ValueWrapper<TValue>(TValue value) : IEquatable<ValueWrapper<TValue>>
   where TValue : IEquatable<TValue>
{
   // ReSharper disable once MemberCanBePrivate.Global
   public TValue Value { get; private set; } = value;

   public bool Equals(ValueWrapper<TValue>? other) => other != null
                                                   && IsConsideredTypeCompatibleForEquality(other)
                                                   && other.Value.Equals(Value);

   public override bool Equals(object? obj) => Equals(obj as ValueWrapper<TValue>);

   protected virtual bool IsConsideredTypeCompatibleForEquality(object other) => GetType() == other.GetType();

   // ReSharper disable once NonReadonlyMemberInGetHashCode only used by serialization, in practice readonly
   public override int GetHashCode() => Value.GetHashCode();
   public override string ToString() => Value.ToStringCE();

   public static bool operator ==(ValueWrapper<TValue>? left, ValueWrapper<TValue>? right) => Equals(left, right);
   public static bool operator !=(ValueWrapper<TValue>? left, ValueWrapper<TValue>? right) => !Equals(left, right);
}
