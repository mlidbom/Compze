using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Compze.Utilities.SystemCE;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Hosting.Testing;

static partial class TestEnv
{
   static class XUnit
   {
      public static class PersistenceLayer
      {
         public static Compze.Wiring.PersistenceLayer Current
         {
            get
            {
               var testName = CurrentTestContext.PluggableComponentsCombination;
               if(testName == null) throw new Exception("XUnit test context not set. Make sure test method has pluggableComponentsCombination parameter and calls TestEnv.SetTestContext.");
               
               var storageProviderName = FindDimensions.Match(testName).Groups[1].Value;
               if(Enum.TryParse(storageProviderName, out Compze.Wiring.PersistenceLayer provider)) return provider;

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
               throw new Exception($"Failed to parse PersistenceLayerProvider from XUnit test environment. Test context was: {testName}, parsed value: {storageProviderName}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
            }
         }

         public static TValue ValueFor<TValue>(TValue msSql = default!, TValue mySql = default!, TValue pgSql = default!) where TValue: notnull
            =>
               Current switch
               {
                  Compze.Wiring.PersistenceLayer.MicrosoftSqlServer => SelectValue(msSql, nameof(msSql)),
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

      public static class DIContainer
      {
         public static Compze.Wiring.DIContainer Current
         {
            get
            {
               var testName = CurrentTestContext.PluggableComponentsCombination;
               if(testName == null) throw new Exception("XUnit test context not set. Make sure test method has pluggableComponentsCombination parameter and calls TestEnv.SetTestContext.");
               
               var containerName = FindDimensions.Match(testName).Groups[2].Value;
               if(!Enum.TryParse(containerName, out Compze.Wiring.DIContainer provider))
               {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                  throw new Exception($"Failed to parse DIContainer from XUnit test environment. Test context was: {testName}, parsed value: {containerName}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
               }

               return provider;
            }
         }
      }

      static readonly Regex FindDimensions = new("""^(.+?):(.+?)$""", RegexOptions.Compiled);
   }
}
