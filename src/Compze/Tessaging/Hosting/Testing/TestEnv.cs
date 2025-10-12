using Compze.Tests.Infrastructure;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
public static partial class TestEnv
{
   internal static readonly IList<Func<PluggableComponents?>> _contextProviders = new List<Func<PluggableComponents?>>();
   static PluggableComponents? GetComponentsFromProviders() => _contextProviders.Select(it => it()).NotNull().FirstOrDefault();

   public static SqlLayer SqlLayer
   {
      get
      {
         if(GetComponentsFromProviders() is {} components)
            return components.SqlLayer;

         // Fall back to NUnit
         var storageProviderName = FindDimensions!.Match(GetNunitTestName()).Groups[1].Value;
         if(Enum.TryParse(storageProviderName, out SqlLayer provider)) return provider;

         throw new Exception($"Failed to parse SqlLayerProvider from test environment. Value was: {storageProviderName}");
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

   public static DIContainer DIContainer
   {
      get
      {
         if(GetComponentsFromProviders() is {} components)
            return components.DiContainer;

         var containerName = FindDimensions.Match(GetNunitTestName()).Groups[2].Value;
         if(!Enum.TryParse(containerName, out Compze.Wiring.DIContainer provider))
         {
            ConsoleCE.WriteImportantLine("DIContainer.Current");
            throw new Exception($"Failed to parse DIContainer from test environment. Value was: {containerName}");
         }

         return provider;
      }
   }
}
