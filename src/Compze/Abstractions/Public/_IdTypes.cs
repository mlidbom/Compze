using System;
using Compze.Core.Public.Infrastructure;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;

namespace Compze.Core.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(Assert.Argument.Is(!Equals(primitiveValue, default(TPrimitive))).then(primitiveValue))
   where TPrimitive : IEquatable<TPrimitive>
{
}

public class TentityId<TPrimitive>(TPrimitive primitiveValue) : EntityId<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive>
{}

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

public class TessageId(Guid id) : TentityId(id)
{
   public TessageId() : this(Guid.CreateVersion7()) {}
}
