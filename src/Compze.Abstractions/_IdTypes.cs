using System.Text.Json.Serialization;
using Compze.Abstractions.Infrastructure;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions;

[JsonConverter(typeof(EntityIdJsonConverter))]
public class EntityId<TPrimitive>(TPrimitive value) : ValueWrapper<TPrimitive>(Argument.Assert(!Equals(value, default(TPrimitive))).__(value))
   where TPrimitive : IEquatable<TPrimitive>
{
   protected override bool IsConsideredTypeCompatibleForEquality(object other) => GetType().IsAssignableToOrFrom(other.GetType());
}

[JsonConverter(typeof(EntityIdJsonConverter))]
public class EntityId(Guid value) : EntityId<Guid>(value)
{
   ///<summary>A new <see cref="EntityId"/>, of a new random <see cref="Guid"/>.</summary>
   public static EntityId New() => new(Guid.NewGuid());
}

[JsonConverter(typeof(EntityIdJsonConverter))]
public class TentityId(Guid value) : EntityId(value)
{
   ///<summary>A new <see cref="TentityId"/>, of a new random <see cref="Guid"/>.</summary>
   public new static TentityId New() => new(Guid.NewGuid());
}

[JsonConverter(typeof(EntityIdJsonConverter))]
public class TaggregateId(Guid value) : TentityId(value)
{
   ///<summary>A new <see cref="TaggregateId"/>, of a new random <see cref="Guid"/>.</summary>
   public new static TaggregateId New() => new(Guid.NewGuid());
}
