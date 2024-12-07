using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Compze.Testing.TestFrameworkExtensions.NUnit;

[Obsolete("""
          This attribute "works", but, with Rider's test runner at least, the tests still show up in the browser and pollute the number of found tests. 
          They don't run, but that's all.
          Definitely prefer XUnit v3 and the XFact attribute which does not have these issues. 
          """)]
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
