using System;
using Compze.Core.Public;

namespace Compze.Core.Refactoring.Naming.Internal;

public class TypeId(Guid guidValue) : EntityId(guidValue)
{
   public TypeId() : this(Guid.NewGuid()) {}
}