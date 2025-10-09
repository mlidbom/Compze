using System;
using System.Runtime.CompilerServices;
using FluentAssertions;

namespace Compze.Tests.Unit.XUnit;

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
}
