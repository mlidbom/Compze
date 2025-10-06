using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Compze.Testing.TestFrameworkExtensions.NUnit;

///<summary>
/// This attribute will run the test eXclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
///This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
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
