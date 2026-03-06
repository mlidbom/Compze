using Compze.Abstractions.Public;
using JetBrains.Annotations;

namespace Compze.Abstractions.Refactoring.Naming.Internal;

public class TypeId(Guid guidValue) : EntityId(guidValue)
{
   [Obsolete("Serializer only", error: true)]
   [UsedImplicitly] public TypeId() : this(Guid.NewGuid()) {}
}
