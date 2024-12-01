using System;
// ReSharper disable UnusedMethodReturnValue.Global

namespace Compze.Contracts;

///<summary>Performs inspections on Guid instances</summary>
static class GuidInspector
{
   ///<summary>Throws a <see cref="GuidIsEmptyContractViolationException"/> if any inspected value is Guid.Empty</summary>
   public static IInspected<Guid> NotEmpty(this IInspected<Guid> me)
   {
      return me.Inspect(
         inspected => inspected != Guid.Empty,
         badValue => new GuidIsEmptyContractViolationException(badValue));
   }
}