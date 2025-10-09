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
            var storageProviderName = FindDimensions.Match(GetTestName()).Groups[1].Value;
            if(Enum.TryParse(storageProviderName, out Compze.Wiring.PersistenceLayer provider)) return provider;

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
            throw new Exception($"Failed to parse PersistenceLayerProvider from test environment. Value was: {storageProviderName}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
         }
      }

      public static TValue ValueFor<TValue>(TValue db2 = default!, TValue memory = default!, TValue msSql = default!, TValue mySql = default!, TValue orcl = default!, TValue pgSql = default!) where TValue: notnull
         =>
            Current switch
            {
               Compze.Wiring.PersistenceLayer.MicrosoftSqlServer => SelectValue(msSql, nameof(msSql)),
               Compze.Wiring.PersistenceLayer.Memory             => SelectValue(memory, nameof(memory)),
               Compze.Wiring.PersistenceLayer.MySql              => SelectValue(mySql, nameof(mySql)),
               Compze.Wiring.PersistenceLayer.PostgreSql         => SelectValue(pgSql, nameof(pgSql)),
               _                                                 => throw new ArgumentOutOfRangeException()
            };

      [return:NotNull]static TValue SelectValue<TValue>(TValue value, string provider)
      {
         if(!Equals(value, default(TValue))) return Result.ReturnNotNull(value);

         throw new Exception($"Value missing for {provider}");
      }
   }

   static string GetTestName()
   {
      //We do not want to reference NUnit so dig this data out through reflection. When running tests NUnit will be there.
      var currentContext = TestContextType.GetProperty("CurrentContext")!.GetMethod!.Invoke(null, null)!;
      var test = currentContext.GetType().GetProperty("Test")!.GetMethod!.Invoke(currentContext, null)!;
      var testName = (string)test.GetType().GetProperty("FullName")!.GetMethod!.Invoke(test, null)!;
      return testName;
   }

   static readonly Type TestContextType = AppDomain.CurrentDomain
                                                   .GetAssemblies()
                                                   .Single(ass => ass.GetName().FullName.ContainsInvariant("nunit.framework"))
                                                   .GetType("NUnit.Framework.TestContext")!;

   static readonly Regex FindDimensions = new("""\("(.*)\:(.*)"\)""", RegexOptions.Compiled);
   public static class DIContainer
   {
      public static Compze.Wiring.DIContainer Current
      {
         get
         {
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
}