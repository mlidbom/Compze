using System;
using System.Diagnostics.CodeAnalysis;
using Compze.Functional;
using Compze.SystemCE;
using JetBrains.Annotations;

namespace Compze.Contracts;

public static class ContractAssertion
{
   [AssertionMethod] public static Unit That(this IContractAssertion @this,
                                               [AssertionCondition(AssertionConditionType.IS_TRUE)] [DoesNotReturnIf(false)]
                                               bool assertion,
                                               string message) => Unit.From(() =>
   {
      if(message.IsNullEmptyOrWhiteSpace()) throw new ArgumentException(nameof(message));
      if(!assertion) throw new AssertionException(@this.InspectionType, message);
   });
}
