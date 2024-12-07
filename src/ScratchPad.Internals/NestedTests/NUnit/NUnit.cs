using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace ScratchPad.NestedTests.NUnit;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class XTestAttribute : TestAttribute, ISimpleTestBuilder
{
   public new TestMethod BuildFrom(IMethodInfo method, Test? suite)
   {
      if (method.MethodInfo.DeclaringType != method.TypeInfo.Type) return null!;

      var testMethod = base.BuildFrom(method, suite);

      if (method.MethodInfo.IsVirtual && !method.MethodInfo.IsFinal && !method.TypeInfo.Type.IsSealed)
         testMethod.MakeInvalid("TestInThisClassOnlyAttribute is not valid on unsealed virtual methods.");

      return testMethod;
   }
}

[TestFixture]public class NUnit_Outer_scenario
{
   [XTest] public void Outer_fact() => Console.WriteLine(nameof(Outer_fact));

   public class Inner_scenario:NUnit_Outer_scenario
   {
      [XTest] public void Inner_fact() => Console.WriteLine(nameof(Inner_fact));

      public class Inner_inner_scenario : Inner_scenario
      {
         [XTest] public void Inner_inner_fact() => Console.WriteLine(nameof(Inner_fact));

         public class Inner_inner_inner_scenario : Inner_inner_scenario
         {
            [XTest] public void Inner_inner_inner_fact() => Console.WriteLine(nameof(Inner_fact));
         }

         public class Inner_inner_inner_scenario2 : Inner_inner_scenario
         {
            [XTest] public void Inner_inner_inner2_fact() => Console.WriteLine(nameof(Inner_fact));
         }
      }
   }
}