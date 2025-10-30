using System;
using Compze.Core.Public.Infrastructure;

namespace Compze.Core.Public;

public class EntityId<TPrimitive>(TPrimitive primitiveValue) : ValueWrapper<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive> {}

public class EntityId(Guid primitiveValue) : EntityId<Guid>(primitiveValue) {}

public class TentityId<TPrimitive>(TPrimitive primitiveValue) : EntityId<TPrimitive>(primitiveValue)
   where TPrimitive : IEquatable<TPrimitive> {}

public class TentityId(Guid primitiveValue) : TentityId<Guid>(primitiveValue) {}
public class TaggregateId(Guid id) : TentityId(id) {}

public class TessageId(Guid id) : TentityId(id) {}
public class TeventId(Guid id) : TessageId(id) {}
public class TommandId(Guid id) : TessageId(id) {}
