using System;
using System.Linq;
using System.Reflection;
using Composable.SystemCE;
using NUnit.Framework;

namespace Composable.Testing;

[SetUpFixture] public class UniversalUncatchableExceptionsGathererFixture
{
   [OneTimeSetUp] public void UniversalSetup()
   {
      UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      AssertTestInheritsUniversalTestBase();
   }

   [OneTimeTearDown] public void UniversalTeardown()
   {
      //We don't consume here,because some test runners, including NCrunch will not surface teardown exceptions so consuming here would hide them. Without consuming, we may see them on the next test run.
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      if(UncatchableExceptionsGatherer.Exceptions.Any())
      {
         throw new AggregateException(UncatchableExceptionsGatherer.Exceptions);
      }
   }

   void AssertTestInheritsUniversalTestBase()
   {

         var testClasses = GetType().Assembly
                                 .GetTypes()
                                 .Where(IsTestClass);

         var invalidTests = testClasses.Where(t => !typeof(UniversalTestBase).IsAssignableFrom(t)).ToList();

         if (invalidTests.Any())
         {
            var typeList = string.Join(Environment.NewLine, invalidTests.Select(t => t.FullName));
            Assert.Fail($"The following test classes do not inherit from TestBase: {typeList}: Count{invalidTests.Count}");
         }
   }

   static bool IsTestClass(Type type)
   {
      if(type.GetCustomAttributes(typeof(TestFixtureAttribute), true).Any())
         return true;

      return type.GetMethods()
                 .Any(method => method.GetCustomAttributes(typeof(TestAttribute), true).Any());
   }
}
