using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure.NUnit.Logging;
using Compze.Utilities.SystemCE;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

[SetUpFixture] public class UniversalTestFixture
{
   [OneTimeSetUp] public void UniversalSetup()
   {
      try
      {
         License.Accepted = true;
         TestFixtureHelper.PerformSetup(new NUnitTestEnricher());
         AssertTestInheritsUniversalTestBase();
      }
      catch(Exception ex)
      {
         LogInitializationFailure("NUnit.UniversalTestFixture.UniversalSetup", ex);
         throw;
      }
   }

   [OneTimeTearDown] public async Task UniversalTeardown()
   {
      TestFixtureHelper.PerformTeardown();
      await Task.CompletedTask; // Keep async signature for consistency
   }

   void AssertTestInheritsUniversalTestBase()
   {
      TestFixtureHelper.AssertAllTestClassesInheritFromBase(
         GetType().Assembly,
         typeof(UniversalTestBase),
         TestFixtureHelper.IsNUnitTestClass);
   }

   static void LogInitializationFailure(string location, Exception ex)
   {
      try
      {
         var logPath = @"c:\tmp\init_failure.txt";
         Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
         var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
         var message = $"[{timestamp}] INITIALIZATION FAILURE in {location}:{Environment.NewLine}" +
                      $"Exception Type: {ex.GetType().FullName}{Environment.NewLine}" +
                      $"Message: {ex.Message}{Environment.NewLine}" +
                      $"Stack Trace: {ex.StackTrace}{Environment.NewLine}" +
                      $"ToString: {ex}{Environment.NewLine}" +
                      $"==============================================================================={Environment.NewLine}";
         File.AppendAllText(logPath, message);
      }
      catch
      {
         // If logging fails, we can't do much about it
      }
   }
}
