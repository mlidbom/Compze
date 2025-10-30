using System;
using Compze.Core.Public;

namespace Compze.Core.Refactoring.Naming.Internal;

public class TypeId : EntityId
{
   public Guid GuidValue => PrimitiveValue;
   public TypeId(Guid guidValue) : base(guidValue){}

}