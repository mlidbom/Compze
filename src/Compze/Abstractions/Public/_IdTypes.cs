using System;
using Compze.Core.Public.Infrastructure;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;

namespace Compze.Core.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(Assert.Argument.Is(!Equals(primitiveValue, default(TPrimitive))).then(primitiveValue))
   where TPrimitive : IEquatable<TPrimitive>
{
}

public class EntityId : EntityId<Guid>
{
   public EntityId(Guid id) : base(id) {}
   public EntityId() : base(Guid.NewGuid()) {}
}

public class TentityId<TPrimitive>(TPrimitive primitiveValue) : EntityId<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive>
{

}

public class TentityId : EntityId
{
   public TentityId(Guid id) : base(id) {}
   public TentityId() : base(Guid.NewGuid()) {}
}

public class TaggregateId : TentityId
{
   public TaggregateId(Guid id) : base(id) {}
   public TaggregateId() : base(Guid.NewGuid()) {}
}

public class TessageId : TentityId
{
   public TessageId(Guid id) : base(id) {}
   public TessageId() : base(Guid.CreateVersion7()) {}
}
