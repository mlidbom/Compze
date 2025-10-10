using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Compze.Utilities.Logging;
using Compze.Utilities.Logging.Serilog;
using Compze.Utilities.SystemCE;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Common test fixture setup/teardown logic shared between NUnit and XUnit test infrastructure.
/// </summary>
public static class TestFixtureHelper
{
   public static void PerformSetup(ILogEventEnricher? testEnricher = null)
   {
      try
      {
         SetupSerilog(testEnricher);
         UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      }
      catch(Exception ex)
      {
         LogInitializationFailure("TestFixtureHelper.PerformSetup", ex);
         throw;
      }
   }

   public static void PerformTeardown()
   {
      //We don't consume here,because some test runners, including NCrunch, will not surface teardown exceptions, so consuming here would hide them. Without consuming, we may see them on the next test run.
      GCCE.ForceFullGcAllGenerationsAndWaitForFinalizers();
      if(UncatchableExceptionsGatherer.Exceptions.Any())
      {
         throw new AggregateException(UncatchableExceptionsGatherer.Exceptions);
      }

      // Synchronously wait for log to close
      Log.CloseAndFlushAsync().AsTask().GetAwaiter().GetResult();
   }

   static void SetupSerilog(ILogEventEnricher? testEnricher)
   {
      var config = new LoggerConfiguration()
                  .Enrich.WithMachineName()
                  .Enrich.WithExceptionDetails();

      if(testEnricher != null)
         config = config.Enrich.With(testEnricher);

      Log.Logger = config.MinimumLevel.Debug()
                         .WriteTo.Seq("http://192.168.0.11:5341", formatProvider: CultureInfo.InvariantCulture)
                         .WriteTo.Console(formatProvider:CultureInfo.InvariantCulture)
                         .CreateLogger();

      CompzeLogger.LoggerFactoryMethod = SerilogLogger.Create;
   }

   public static void AssertAllTestClassesInheritFromBase(Assembly assembly, Type baseType, Func<Type, bool> isTestClassPredicate)
   {
      try
      {
         var testClasses = assembly.GetTypes().Where(isTestClassPredicate);
         var invalidTests = testClasses.Where(t => !baseType.IsAssignableFrom(t)).ToList();

         if(invalidTests.Any())
         {
            var typeList = string.Join(Environment.NewLine, invalidTests.Select(t => t.FullName));
            throw new InvalidOperationException($"The following test classes do not inherit from {baseType.Name}: {typeList}: Count {invalidTests.Count}");
         }
      }
      catch(Exception ex)
      {
         LogInitializationFailure($"TestFixtureHelper.AssertAllTestClassesInheritFromBase (Assembly: {assembly.GetName().Name})", ex);
         throw;
      }
   }

   public static bool IsXUnitTestClass(Type type)
   {
      if(type.IsAbstract || type.IsInterface)
         return false;

      // Check for XUnit attributes by name to avoid taking a dependency on XUnit in this shared project
      return type.GetMethods()
                 .Any(method => method.GetCustomAttributes(true)
                                      .Any(attr => attr.GetType().Name is "FactAttribute" or "TheoryAttribute"));
   }

   public static bool IsNUnitTestClass(Type type)
   {
      // Check for NUnit attributes by name to avoid taking a dependency on NUnit in this shared project
      if(type.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "TestFixtureAttribute"))
         return true;

      return type.GetMethods()
                 .Any(method => method.GetCustomAttributes(true)
                                      .Any(attr => attr.GetType().Name == "TestAttribute"));
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
