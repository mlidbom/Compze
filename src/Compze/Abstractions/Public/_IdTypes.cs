using System;
using Compze.Core.Public.Infrastructure;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;

namespace Compze.Core.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive>
{
   //urgent: should not allow empty at all. We need to handle uninitialized Ids some other way
   public bool IsEmpty => Equals(PrimitiveValue, default(TPrimitive));
   public unit AssertNotEmpty() => Assert.Invariant.Is(!IsEmpty).then(unit.Value);
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
