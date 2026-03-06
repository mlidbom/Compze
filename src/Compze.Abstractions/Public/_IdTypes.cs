using Compze.Abstractions.Public.Infrastructure;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(Argument.Assert(!Equals(primitiveValue, default(TPrimitive)))._then(primitiveValue))
   where TPrimitive : IEquatable<TPrimitive>
{
   protected override bool IsConsideredTypeCompatibleForEquality(object other) => GetType().IsAssignableToOrFrom(other.GetType());
}

public class EntityId(Guid id) : EntityId<Guid>(id)
{
   public EntityId() : this(Guid.NewGuid()) {}
}

public class TentityId(Guid id) : EntityId(id)
{
   // ReSharper disable once UnusedMember.Global
   public TentityId() : this(Guid.NewGuid()) {}
}

public class TaggregateId(Guid id) : TentityId(id)
{
   public TaggregateId() : this(Guid.NewGuid()) {}
}

public class TessageId(Guid id) : EntityId(id)
{
   public TessageId() : this(Guid.CreateVersion7()) {}
}
