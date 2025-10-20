using System;
using System.Diagnostics.CodeAnalysis;
using Xunit.Sdk;
using Compze.Tessaging.Hosting.Testing;

namespace Compze.Tests.Infrastructure.XUnit;

static class TestCasePluggableComponentsExtractor
{
   public static Tessaging.Hosting.Testing.PluggableComponents ExtractPluggableComponents(this XunitTestCase? testCase) =>
      testCase.TryExtractPluggableComponents(throwOnFailure: true)!.Value;

   public static Tessaging.Hosting.Testing.PluggableComponents? TryExtractPluggableComponents(this XunitTestCase? testCase,
                                                                                              bool throwOnFailure = false)
   {
      if(testCase == null) throw new Exception("No test context has been set");

      var arguments = testCase.TestMethodArguments;
      if(arguments.Length != 1)
      {
         if(throwOnFailure)
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                the arguments array should have a single entry that is a string,
                but it had {arguments.Length} entries
                """);

         return null;
      }

      if(arguments[0].GetType() != typeof(string))
      {
         if(throwOnFailure)
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                the arguments array should have a single entry that is a string,
                but the type was {arguments[0].GetType().FullName}
                """);
         return null;
      }

      var argument = (string)arguments[0];
      try
      {
         return Tessaging.Hosting.Testing.PluggableComponents.FromString(argument);
      }
      catch(Exception ex)
      {
         if(throwOnFailure)
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                could not parse pluggable components from the string {argument}
                """,
               ex);
         return null;
      }
   }
}
