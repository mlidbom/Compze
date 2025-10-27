using System;

namespace Compze.Utilities.GenericAbstractions;

public interface IUntypedEntityId
{
   object UntypedPrimitiveValue { get; }
}

interface IEntityId<out TPrimitive> : IUntypedEntityId
{
   TPrimitive PrimitiveValue { get; }
}

/// <summary>
/// Represents a strongly-typed identifier for a specific entity, encapsulating a value of type <typeparamref
/// name="TPrimitive"/>.
/// </summary>
/// <remarks>This abstract class provides a base for creating strongly-typed identifiers, ensuring type safety and
/// equality comparison. Derived classes can represent specific identifier types, such as <see cref="Guid"/>-based
/// identifiers.</remarks>
/// <typeparam name="TPrimitive">The type of the underlying value used for the identifier. Must be a value type.</typeparam>
/// <param name="primitiveValue"></param>
public abstract class EntityId<TPrimitive>(TPrimitive primitiveValue) : IEntityId<TPrimitive>, IEquatable<EntityId<TPrimitive>>
   where TPrimitive : struct
{
   public TPrimitive PrimitiveValue { get; } = primitiveValue;

   public bool Equals(EntityId<TPrimitive>? other) => PrimitiveValue.Equals(other?.PrimitiveValue);

   public override bool Equals(object? other) => other?.GetType() == GetType() && Equals((EntityId<TPrimitive>)other);

   public override int GetHashCode() => PrimitiveValue.GetHashCode();

   public static bool operator ==(EntityId<TPrimitive>? left, EntityId<TPrimitive>? right) => Equals(left, right);
   public static bool operator !=(EntityId<TPrimitive>? left, EntityId<TPrimitive>? right) => !Equals(left, right);
   object IUntypedEntityId.UntypedPrimitiveValue => PrimitiveValue;
}