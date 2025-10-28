using System;

namespace Compze.Core.Public.Infrastructure;

public class TentityId<TPrimitive>(TPrimitive primitiveValue) : EntityId<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive> {}

public class TentityId(Guid primitiveValue) : TentityId<Guid>(primitiveValue) {}
