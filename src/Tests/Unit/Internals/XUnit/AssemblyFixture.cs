using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.XUnit;

/// <summary>
/// XUnit v2 doesn't have IAssemblyFixture. This class uses a module initializer to perform assembly-level setup.
/// </summary>
public static class AssemblySetup
{
   static bool _initialized;
   static readonly object _lock = new();

   [ModuleInitializer]
   public static void Initialize()
   {
      try
      {
         lock(_lock)
         {
            if(_initialized) return;
            _initialized = true;

            License.Accepted = true;
            Tests.Infrastructure.TestFixtureHelper.PerformSetup();
            AssertTestInheritsUniversalTestBase();

            // Register cleanup for when the AppDomain unloads
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
         }
      }
      catch(Exception ex)
      {
         LogInitializationFailure("Unit.Internals.XUnit.AssemblySetup.Initialize", ex);
         throw;
      }
   }

   static void OnProcessExit(object? sender, EventArgs e) => Cleanup();
   static void OnDomainUnload(object? sender, EventArgs e) => Cleanup();

   static void Cleanup()
   {
      Tests.Infrastructure.TestFixtureHelper.PerformTeardown();
   }

   static void AssertTestInheritsUniversalTestBase()
   {
      Tests.Infrastructure.TestFixtureHelper.AssertAllTestClassesInheritFromBase(
         typeof(AssemblySetup).Assembly,
         typeof(Tests.Infrastructure.XUnit.UniversalTestBase),
         Tests.Infrastructure.TestFixtureHelper.IsXUnitTestClass);
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
