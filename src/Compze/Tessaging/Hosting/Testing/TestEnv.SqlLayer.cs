using Compze.Utilities.SystemCE;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
static partial class TestEnv
{
   static (Wiring.SqlLayer, Wiring.DIContainer) ParseParts(string _combination)
   {
      try
      {
         var parts = _combination.Split(':');

         Argument.Is(parts.Length == 2, () => $"PluggableComponentParts has an invalid format: {_combination}");

         return ((Wiring.SqlLayer)Enum.Parse(typeof(Wiring.SqlLayer), parts[0]),
                 (Wiring.DIContainer)Enum.Parse(typeof(Wiring.DIContainer), parts[1]));
      }
      catch(Exception e)
      {
         throw new Exception($"PluggableComponentParts has an invalid format: {_combination}", e);
      }
   }

   ///<summary>Sql layer members</summary>
   public static class SqlLayer
   {
      static readonly LazyStruct<Compze.Wiring.SqlLayer> _cache = new LazyStruct<Compze.Wiring.SqlLayer>(GetCurrent);
      public static Compze.Wiring.SqlLayer Current => _cache.Value;

      public static Compze.Wiring.SqlLayer GetCurrent()
      {
         if(XUnitTestContext.PluggableComponentsCombination != null)
         {
            return XUnit.XUnitSqlLayer.Current;
         }

         // Fall back to NUnit
         var storageProviderName = FindDimensions.Match(GetNunitTestName()).Groups[1].Value;
         if(Enum.TryParse(storageProviderName, out Compze.Wiring.SqlLayer provider)) return provider;

         throw new Exception($"Failed to parse SqlLayerProvider from test environment. Value was: {storageProviderName}");
      }

      public static TValue ValueFor<TValue>(TValue msSql, TValue mySql, TValue pgSql, TValue sqlite) where TValue : notnull
      {
         return Current switch
         {
            Wiring.SqlLayer.MicrosoftSqlServer => msSql,
            Wiring.SqlLayer.MySql              => mySql,
            Wiring.SqlLayer.PostgreSql         => pgSql,
            Wiring.SqlLayer.Sqlite             => sqlite,
            Wiring.SqlLayer.SqliteMemory       => sqlite,
            _                                  => throw new ArgumentOutOfRangeException()
         };
      }
   }

   static string GetNunitTestName()
   {
      var currentContext = GetNUnitTestContextType().GetProperty("CurrentContext")!.GetMethod!.Invoke(null, null)!;
      var test = currentContext.GetType().GetProperty("Test")!.GetMethod!.Invoke(currentContext, null)!;
      var testName = (string)test.GetType().GetProperty("FullName")!.GetMethod!.Invoke(test, null)!;
      return testName;
   }

   static Type GetNUnitTestContextType()
   {
      //We do not want to reference NUnit so dig this data out through reflection. When running tests NUnit will be there.
      return AppDomain.CurrentDomain
                      .GetAssemblies()
                      .Single(ass => ass.GetName().FullName.ContainsInvariant("nunit.framework"))
                      .GetType("NUnit.Framework.TestContext")
                      .NotNull();
   }

   static readonly Regex FindDimensions = new("""\("(.*)\:(.*)"\)""", RegexOptions.Compiled);

   public static class DIContainer
   {
      public static Compze.Wiring.DIContainer Current
      {
         get
         {
            if(XUnitTestContext.PluggableComponentsCombination != null)
            {
               return XUnit.XUnitDIContainer.Current;
            }

            var containerName = FindDimensions.Match(GetNunitTestName()).Groups[2].Value;
            if(!Enum.TryParse(containerName, out Compze.Wiring.DIContainer provider))
            {
               throw new Exception($"Failed to parse DIContainer from test environment. Value was: {containerName}");
            }

            return provider;
         }
      }
   }



   static class XUnitTestContext
   {
      [ThreadStatic]
      public static string? PluggableComponentsCombination;
   }

   /// <summary>
   /// Call this at the beginning of XUnit test methods that use pluggable components.
   /// This sets up the context so that TestEnv.SqlLayer.Current and TestEnv.DIContainer.Current work correctly.
   /// </summary>
   public static void SetXunitTestContext(string pluggableComponentsCombination) =>
      XUnitTestContext.PluggableComponentsCombination = pluggableComponentsCombination;
}
