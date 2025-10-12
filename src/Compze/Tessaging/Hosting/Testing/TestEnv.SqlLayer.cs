using Compze.Tests.Infrastructure;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
public static partial class TestEnv
{
   static readonly LazyStruct<SqlLayer> SqlLayerCache = new(() =>
   {
      if(_xUnitPluggableComponentsCombination != null)
      {
         return XUnit.XUnitSqlLayer.Current;
      }

      // Fall back to NUnit
      var storageProviderName = FindDimensions!.Match(GetNunitTestName()).Groups[1].Value;
      if(Enum.TryParse(storageProviderName, out SqlLayer provider)) return provider;

      throw new Exception($"Failed to parse SqlLayerProvider from test environment. Value was: {storageProviderName}");
   });

   public static SqlLayer SqlLayer => SqlLayerCache.Value;

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

   static readonly LazyStruct<DIContainer> ContainerCache = new(() =>
   {
      if(_xUnitPluggableComponentsCombination != null)
      {
         return XUnit.XUnitDIContainer.Current;
      }

      var containerName = FindDimensions.Match(GetNunitTestName()).Groups[2].Value;
      if(!Enum.TryParse(containerName, out Compze.Wiring.DIContainer provider))
      {
         ConsoleCE.WriteImportantLine("DIContainer.Current");
         throw new Exception($"Failed to parse DIContainer from test environment. Value was: {containerName}");
      }

      return provider;
   });

   public static DIContainer DIContainer => ContainerCache.Value;

   static PluggableComponents? _xUnitPluggableComponentsCombination;

   internal static void SetXunitTestContext(PluggableComponents pluggableComponentsCombination) =>
      _xUnitPluggableComponentsCombination = pluggableComponentsCombination;
}
