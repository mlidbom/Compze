using System;
using Compze.Core.Public.Infrastructure;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;

namespace Compze.Core.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive> {}

public class EntityId(Guid primitiveValue) : EntityId<Guid>(Assert.Argument.Is(primitiveValue != Guid.Empty).then(primitiveValue)) {}

public class TentityId<TPrimitive>(TPrimitive primitiveValue) : EntityId<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive> {}

public class TentityId(Guid primitiveValue) : TentityId<Guid>(primitiveValue) {}
public class TaggregateId(Guid id) : TentityId(id) {}

public class TessageId : TentityId
{
   public TessageId(Guid id) : base(id) {}
   public TessageId() : base(Guid.CreateVersion7()) {}
}
