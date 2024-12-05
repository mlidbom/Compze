using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;
#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Compze.Contracts;

static class Assert
{
   ///<summary>Assert conditions about current state of "this". Failures would mean that someone made a call that is illegal given state of "this".</summary>
   public static BaseAssertion State { get; } = BaseAssertion.StateInstance;

   ///<summary>Assert something that must always be true for "this".</summary>
   public static BaseAssertion Invariant { get; } = BaseAssertion.InvariantInstance;

   ///<summary>Assert conditions on arguments to current method.</summary>
   public static BaseAssertion Argument { get; } = BaseAssertion.ArgumentsInstance;

   ///<summary>Assert conditions on the result of making a method call.</summary>
   public static BaseAssertion Result { get; } = BaseAssertion.ResultInstance;



   public readonly struct BaseAssertion
   {
      internal static BaseAssertion InvariantInstance = new(InspectionType.Invariant);
      internal static BaseAssertion ArgumentsInstance = new(InspectionType.Argument);
      internal static BaseAssertion StateInstance = new(InspectionType.State);
      internal static BaseAssertion ResultInstance = new(InspectionType.Result);

      readonly InspectionType _inspectionType;
      BaseAssertion(InspectionType inspectionType) => _inspectionType = inspectionType;

      [ContractAnnotation("c1:false => halt")] public BaseAssertion Is([DoesNotReturnIf(false)]bool c1) => RunAssertions(_inspectionType, c1);


      [return: NotNull] [ContractAnnotation("obj:null => halt")]
      public TValue NotNull<TValue>(TValue? obj) => obj ?? throw new AssertionException(_inspectionType, 0);

      [return: NotNull] [ContractAnnotation("obj:null => halt")]
      public TValue NotNullOrDefault<TValue>(TValue? obj)
      {
         if(NullOrDefaultTester<TValue>.IsNullOrDefault(obj))
         {
            throw new AssertionException(_inspectionType, 0);
         }

         return obj!;
      }

      BaseAssertion RunAssertions(InspectionType inspectionType, params bool[] conditions)
      {
         for (var condition = 0; condition < conditions.Length; condition++)
         {
            if (!conditions[condition])
            {
               throw new AssertionException(inspectionType, condition);
            }
         }
         return this;
      }
   }


   public class AssertionException : Exception
   {
      public AssertionException(InspectionType inspectionType, int index) : base($"{inspectionType}: {index}") { }
   }
}