using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using System;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());

      TestEnv.XunitDiscoverer = () =>
      {
         var testCase = TestContext.CurrentTestCase;
         if(testCase == null) throw new Exception("No test context has been set");

         var arguments = testCase.TestMethodArguments;
         if(arguments.Length != 1)
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                the arguments array should have a single entry that is a string,
                but it had {arguments.Length} entries
                """);

         if(arguments[0].GetType() != typeof(string))
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                the arguments array should have a single entry that is a string,
                but the type was {arguments[0].GetType().FullName}
                """);

         var argument = (string)arguments[0];
         try
         {
            return Tessaging.Hosting.Testing.PluggableComponents.FromString(argument);
         }
         catch(Exception ex)
         {
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                could not parse pluggable components from the string {argument}
                """);
         }
      };
   }
}
