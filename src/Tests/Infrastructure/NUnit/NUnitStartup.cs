using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Utilities.Logging;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

public class NUnitStartupInfrastructure
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(null);
      TestEnv.NunitDiscoverer = () =>
      {
         var testName = TestContext.CurrentContext.Test.FullName;
         var matches = FindDimensions.Match(testName);
         var sqlLayerName = matches.Groups[1].Value;
         var containerName = matches.Groups[2].Value;

         return PluggableComponents.FromStrings(sqlLayerName, containerName);
      };
   }

   static readonly Regex FindDimensions = new("""\("(.*)\:(.*)"\)""", RegexOptions.Compiled);
}
