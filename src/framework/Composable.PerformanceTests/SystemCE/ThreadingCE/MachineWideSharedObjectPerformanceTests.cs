using System;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.SystemCE.ThreadingCE;

[TestFixture] public class MachineWideSharedObjectPerformanceTests
{
   [Test] public void Get_copy_runs_single_threaded_100_times_in_40_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      using var shared = MachineWideSharedObject<SharedObject>.For(name);
      using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
      TimeAsserter.Execute(() => shared.GetCopy(), iterations: 100, maxTotal: 40.Milliseconds());
      TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 100, maxTotal: 40.Milliseconds());
   }

   [Test] public void Get_copy_runs_multi_threaded_100_times_in_30_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      using var shared = MachineWideSharedObject<SharedObject>.For(name);
      using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
      TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 100, maxTotal: 30.Milliseconds());
      TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 100, maxTotal: 30.Milliseconds());
   }

   [Test] public void Update_runs_single_threaded_100_times_in_80_milliseconds()
   {
      MachineWideSharedObject<SharedObject> shared = null!;
      var counter = 0;

      TimeAsserter.Execute(
         setup: () =>
         {
            counter = 0;
            shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());
         },
         tearDown: () =>
         {
            shared.GetCopy().Name.Should().Be("100");
            shared.Dispose();
         },
         action: () => shared.Update(@this => @this.Name = (++counter).ToString()),
         iterations: 100,
         maxTotal: 70.Milliseconds());
   }

   [Test] public void Update_runs_multi_threaded_100_times_in_80_milliseconds()
   {
      MachineWideSharedObject<SharedObject> shared = null!;
      var counter = 0;

      TimeAsserter.ExecuteThreaded(
         setup: () =>
         {
            counter = 0;
            shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());
         },
         tearDown: () =>
         {
            shared.GetCopy().Name.Should().Be("100");
            shared.Dispose();
         },
         action: () => shared.Update(@this => @this.Name = (++counter).ToString()),
         iterations: 100,
         maxTotal: 80.Milliseconds());
   }
}