using System;
using Compze.Core.Public;

namespace Compze.Core.Refactoring.Naming.Internal;

class TypeId : EntityId
{
   public Guid GuidValue => PrimitiveValue;
   public TypeId(Guid guidValue) : base(guidValue){}

}