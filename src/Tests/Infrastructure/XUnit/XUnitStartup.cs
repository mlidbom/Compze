using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Logging;
using Compze.Utilities.Threading.ResourceAccess;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());
      TestEnv.XunitDiscoverer = () => TestContext.Current?.PluggableComponents ?? throw new Exception("No pluggable components set for current test");

      var monitorLogger = CompzeLogger.For(typeof(MonitorCE));
      CompzeLogger.For(typeof(XUnitInfrastructureModuleInitializer)).Log().Warning("Registering to log for MonitorCE");
      MonitorCE.OnTimeOut = () =>
      {
         try
         {
            throw new Exception("Lock timed out");
         }
         catch(Exception ex)
         {
            monitorLogger.Error(ex, $"""
                                     Lock timed out
                                     
                                     Stacktrace:
                                     
                                     {new StackTrace(fNeedFileInfo: true)}
                                     """);
         }
      };
   }
}
