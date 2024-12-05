using System.Diagnostics.CodeAnalysis;
using Compze.Contracts.Deprecated;
using JetBrains.Annotations;

namespace Compze.Contracts;

readonly struct BaseAssertion
{
   internal static BaseAssertion InvariantInstance = new(InspectionType.Invariant);
   internal static BaseAssertion ArgumentsInstance = new(InspectionType.Argument);
   internal static BaseAssertion StateInstance = new(InspectionType.State);
   internal static BaseAssertion ResultInstance = new(InspectionType.Result);

   readonly InspectionType _inspectionType;
   BaseAssertion(InspectionType inspectionType) => _inspectionType = inspectionType;

   [ContractAnnotation("c1:false => halt")] public BaseAssertion Is([DoesNotReturnIf(false)]bool c1) => RunAssertions(_inspectionType, c1);


   public TValue NotNull<TValue>(TValue? obj) => obj ?? throw new Assert.AssertionException(_inspectionType, 0);

   [return: System.Diagnostics.CodeAnalysis.NotNull] [ContractAnnotation("obj:null => halt")]
   public TValue NotNullOrDefault<TValue>(TValue? obj)
   {
      if(NullOrDefaultTester<TValue>.IsNullOrDefault(obj))
      {
         throw new Assert.AssertionException(_inspectionType, 0);
      }

      return obj!;
   }

   BaseAssertion RunAssertions(InspectionType inspectionType, params bool[] conditions)
   {
      for (var condition = 0; condition < conditions.Length; condition++)
      {
         if (!conditions[condition])
         {
            throw new Assert.AssertionException(inspectionType, condition);
         }
      }
      return this;
   }
}
