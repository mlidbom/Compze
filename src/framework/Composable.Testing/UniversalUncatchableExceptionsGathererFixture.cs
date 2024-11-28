using System;
using System.Linq;
using Composable.SystemCE;
using NUnit.Framework;

namespace Composable.Testing;

[SetUpFixture] public class UniversalUncatchableExceptionsGathererFixture
{
   [OneTimeSetUp] public void UniversalSetup()
   {
      //For now, we don't consume here because we want failures to be really in your face until we're sure this stuff works right.
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      if(UncatchableExceptionsGatherer.Exceptions.Any())
      {
         throw new AggregateException(UncatchableExceptionsGatherer.Exceptions);
      }
   }

   [OneTimeTearDown] public void UniversalTeardown()
   {
      //We don't consume here,because some test runners, including NCrunch will not surface teardown exceptions.
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      if(UncatchableExceptionsGatherer.Exceptions.Any())
      {
         throw new AggregateException(UncatchableExceptionsGatherer.Exceptions);
      }
   }
}
