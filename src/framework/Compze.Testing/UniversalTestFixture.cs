using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Compze.Logging;
using Compze.Logging.Serilog;
using Compze.SystemCE;
using Compze.Testing.Logging.Serilog;
using NUnit.Framework;
using Serilog;
using Serilog.Exceptions;

namespace Compze.Testing;

[SetUpFixture] public class UniversalTestFixture
{
   [OneTimeSetUp] public void UniversalSetup()
   {
      SetupSerilog();
      UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      AssertTestInheritsUniversalTestBase();
   }

   [OneTimeTearDown] public async Task UniversalTeardown()
   {
      //We don't consume here,because some test runners, including NCrunch will not surface teardown exceptions so consuming here would hide them. Without consuming, we may see them on the next test run.
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      if(UncatchableExceptionsGatherer.Exceptions.Any())
      {
         throw new AggregateException(UncatchableExceptionsGatherer.Exceptions);
      }

      await Log.CloseAndFlushAsync();
   }

   static void SetupSerilog()
   {
      Log.Logger = new LoggerConfiguration()
                  .Enrich.WithMachineName()
                  .Enrich.WithExceptionDetails()
                  .Enrich.With<NUnitTestEnricher>()
                  .MinimumLevel.Debug()
                  .WriteTo.Seq("http://192.168.0.11:5341")
                  .WriteTo.Console(formatProvider:CultureInfo.InvariantCulture)
                  .CreateLogger();

      CompzeLogger.LoggerFactoryMethod = SerilogLogger.Create;
   }

   void AssertTestInheritsUniversalTestBase()
   {
      var testClasses = GetType().Assembly
                                 .GetTypes()
                                 .Where(IsTestClass);

      var invalidTests = testClasses.Where(t => !typeof(UniversalTestBase).IsAssignableFrom(t)).ToList();

      if(invalidTests.Any())
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
