using System;

namespace Compze.Core.Public.Infrastructure;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive> {}

public class EntityId(Guid primitiveValue) : EntityId<Guid>(primitiveValue) {}
