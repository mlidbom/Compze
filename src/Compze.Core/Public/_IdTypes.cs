using System;
using Compze.Core.Public.Infrastructure;
using Compze.Contracts;
using Compze.Functional;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Core.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(ContractAssertion.Argument.Fulfills(!Equals(primitiveValue, default(TPrimitive)))._then(primitiveValue))
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
