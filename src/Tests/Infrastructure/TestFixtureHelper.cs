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
      catch
      {
         LogFailure(typeof(TestFixtureHelper));
         throw;
      }
   }

   public static void PerformTeardown()
   {
      try
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
      catch
      {
         LogFailure(typeof(TestFixtureHelper));
         throw;
      }
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
                         .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
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
      catch
      {
         LogFailure(typeof(TestFixtureHelper));
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

   public static void LogFailure(Type type)
   {
      try
      {
         File.AppendAllText(@"c:\tmp\init_failure.txt", type.FullName + Environment.NewLine);
      }
      catch
      {
         // If logging fails, ignore it
      }
   }

   public static void RunAssemblyLevelSetup<TRunner>(Action action) => RunAssemblyLevelAction<TRunner>(action, "Setup");

   public static void RunAssemblyLevelTeardown<TRunner>(Action action) => RunAssemblyLevelAction<TRunner>(action, "Teardown");

   static void RunAssemblyLevelAction<TRunner>(Action action, string actionType)
   {
      try
      {
         action();
      }
      catch(Exception e)
      {
         File.AppendAllText(@"c:\tmp\init_failure.txt",
                            @$"{actionType}Failure: {typeof(TRunner).FullName}
Exception: {e}
");
         throw;
      }
   }
}
