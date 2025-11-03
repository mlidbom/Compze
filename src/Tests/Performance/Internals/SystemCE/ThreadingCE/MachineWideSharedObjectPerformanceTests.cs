using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;
using System;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

public class MachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   readonly IServiceLocator _serviceLocator;
   readonly MachineWideSharedObject<SharedObject> _shared;

   public MachineWideSharedObjectPerformanceTests()
   {
      _serviceLocator = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents().ServiceLocator;
      _shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString(), _serviceLocator.Resolve<ISharedObjectSerializer>());
   }

   protected override void DisposeInternal()
   {
      _serviceLocator.Dispose();
      _shared.Delete();
   }

   [PCTSerializer] public void Get_copy_runs_single_threaded_XX_times_in_50_milliseconds()
      => TimeAsserter.Execute(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [PCTSerializer] public void Get_copy_runs_multi_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [PCTSerializer] public void Update_runs_single_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.Execute(() => _shared.Update(it => it.Name = ""), iterations: 20, maxTotal: 50.Milliseconds(), maxTries: 10);

   [PCTSerializer] public void Update_runs_multi_threaded_30_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Update(it => it.Name = ""), iterations: 30, maxTotal: 50.Milliseconds(), maxTries: 10);
}
