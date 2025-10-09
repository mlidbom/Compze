using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Compze.Utilities.SystemCE;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
static partial class TestEnv
{
   ///<summary>Persistence layer members</summary>
   public static class PersistenceLayer
   {
      public static Compze.Wiring.PersistenceLayer Current
      {
         get
         {
            // Check if we're running in XUnit context first (via thread-local storage)
            if(CurrentTestContext.PluggableComponentsCombination != null)
            {
               return XUnit.PersistenceLayer.Current;
            }

            // Fall back to NUnit
            var storageProviderName = FindDimensions.Match(GetTestName()).Groups[1].Value;
            if(Enum.TryParse(storageProviderName, out Compze.Wiring.PersistenceLayer provider)) return provider;

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
            throw new Exception($"Failed to parse PersistenceLayerProvider from test environment. Value was: {storageProviderName}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
         }
      }

      public static TValue ValueFor<TValue>(TValue db2 = default!, TValue memory = default!, TValue msSql = default!, TValue mySql = default!, TValue orcl = default!, TValue pgSql = default!) where TValue: notnull
      {
         // Check if we're running in XUnit context first
         if(CurrentTestContext.PluggableComponentsCombination != null)
         {
            return XUnit.PersistenceLayer.ValueFor(db2: db2, memory: memory, msSql: msSql, mySql: mySql, orcl: orcl, pgSql: pgSql);
         }

         // Fall back to NUnit
         return Current switch
         {
            Compze.Wiring.PersistenceLayer.MicrosoftSqlServer => SelectValue(msSql, nameof(msSql)),
            Compze.Wiring.PersistenceLayer.Memory             => SelectValue(memory, nameof(memory)),
            Compze.Wiring.PersistenceLayer.MySql              => SelectValue(mySql, nameof(mySql)),
            Compze.Wiring.PersistenceLayer.PostgreSql         => SelectValue(pgSql, nameof(pgSql)),
            _                                                 => throw new ArgumentOutOfRangeException()
         };
      }

      [return:NotNull]static TValue SelectValue<TValue>(TValue value, string provider)
      {
         if(!Equals(value, default(TValue))) return Result.ReturnNotNull(value);

         throw new Exception($"Value missing for {provider}");
      }
   }

   static string GetTestName()
   {
      //We do not want to reference NUnit so dig this data out through reflection. When running tests NUnit will be there.
      var currentContext = GetNUnitTestContextType().GetProperty("CurrentContext")!.GetMethod!.Invoke(null, null)!;
      var test = currentContext.GetType().GetProperty("Test")!.GetMethod!.Invoke(currentContext, null)!;
      var testName = (string)test.GetType().GetProperty("FullName")!.GetMethod!.Invoke(test, null)!;
      return testName;
   }

   static Type? _testContextType;
   static Type GetNUnitTestContextType()
   {
      if(_testContextType != null) return _testContextType;
      
      _testContextType = AppDomain.CurrentDomain
                                   .GetAssemblies()
                                   .Single(ass => ass.GetName().FullName.ContainsInvariant("nunit.framework"))
                                   .GetType("NUnit.Framework.TestContext")!;
      return _testContextType;
   }

   static readonly Regex FindDimensions = new("""\("(.*)\:(.*)"\)""", RegexOptions.Compiled);
   
   public static class DIContainer
   {
      public static Compze.Wiring.DIContainer Current
      {
         get
         {
            // Check if we're running in XUnit context first
            if(CurrentTestContext.PluggableComponentsCombination != null)
            {
               return XUnit.DIContainer.Current;
            }

            // Fall back to NUnit
            var containerName = FindDimensions.Match(GetTestName()).Groups[2].Value;
            if(!Enum.TryParse(containerName, out Compze.Wiring.DIContainer provider))
            {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
               throw new Exception($"Failed to parse DIContainer from test environment. Value was: {containerName}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
            }

            return provider;
         }
      }
   }

   /// <summary>
   /// Thread-local storage for current test context in XUnit.
   /// Set this at the beginning of each test method.
   /// </summary>
   [ThreadStatic]
   static string? _currentPluggableComponentsCombination;

   static class CurrentTestContext
   {
      public static string? PluggableComponentsCombination
      {
         get => _currentPluggableComponentsCombination;
         set => _currentPluggableComponentsCombination = value;
      }
   }

   /// <summary>
   /// Call this at the beginning of XUnit test methods that use pluggable components.
   /// This sets up the context so that TestEnv.PersistenceLayer.Current and TestEnv.DIContainer.Current work correctly.
   /// </summary>
   public static void SetTestContext(string pluggableComponentsCombination)
   {
      CurrentTestContext.PluggableComponentsCombination = pluggableComponentsCombination;
   }
}