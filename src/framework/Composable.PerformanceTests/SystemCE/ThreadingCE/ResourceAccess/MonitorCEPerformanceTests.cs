using System;
using Composable.Logging;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing;
using Composable.Testing.Performance;
using NUnit.Framework;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable UnusedParameter.Local

// ReSharper disable InconsistentlySynchronizedField

namespace Composable.Tests.SystemCE.ThreadingCE.ResourceAccess;

public class MonitorCEPerformanceTests : UniversalTestBase
{
   class MyLong
   {
      public long Value { get; private set; }

      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

      internal long Read_Unsafe() => Value;

      internal long Read_Locked()
      {
         lock(_monitor) return Read_Unsafe();
      }

      internal long Read_MonitorCE_Using_EnterLock()
      {
         using(_monitor.EnterLock())
         {
            return Read_Unsafe();
         }
      }

      internal long Read_MonitorCE_Read() => _monitor.Read(Read_Unsafe);


      internal void Increment_Unsafe() => Value++;

      internal void Increment_Locked()
      {
         lock(_monitor) Increment_Unsafe();
      }

      internal void Increment_MonitorCE_Using_EnterLock()
      {
         using(_monitor.EnterLock()) Increment_Unsafe();
      }

      internal void Increment_MonitorCE_Update() => _monitor.Update(Increment_Unsafe);
   }

   MyLong _guarded;
   static readonly long TotalLocks = 10_000_000.EnvDivide(unoptimized: 10, instrumented: 100);
   const int Iterations = 100;
   static readonly long LocksPerIteration = TotalLocks / Iterations;

   [SetUp] public void WarmUp()
   {
      _guarded = new MyLong();
      _guarded.Read_Unsafe();
      _guarded.Read_Locked();
      _guarded.Read_MonitorCE_Using_EnterLock();
      _guarded.Read_MonitorCE_Read();
      _guarded.Increment_Unsafe();
      _guarded.Increment_Locked();
      _guarded.Increment_MonitorCE_Using_EnterLock();
      _guarded.Increment_MonitorCE_Update();
   }

   [TearDown] public void TearDownTask() => ConsoleCE.WriteImportantLine(StringCE.Invariant($"{_guarded.Value:N0}"));

   static void RunSingleThreadedScenario(Action action, TimeSpan singleThreadMaxTime)
   {
      //ncrunch: no coverage start
      void HammerScenario()
      {
         for(var i = 0; i < TotalLocks; i++)
            action();
      }
      //ncrunch: no coverage end

      TimeAsserter.Execute(HammerScenario, description: "Singlethreaded", maxTotal: singleThreadMaxTime);
   }

   static void RunMultiThreadedScenario(Action action, TimeSpan multiThreadAllowedTime)
   {
      //ncrunch: no coverage start
      void HammerScenario()
      {
         for(var i = 0; i < LocksPerIteration; i++)
            action();
      }
      //ncrunch: no coverage end

      TimeAsserter.ExecuteThreadedLowOverhead(HammerScenario, Iterations, description: "Multithreaded", maxTotal: multiThreadAllowedTime);
   }

   // ReSharper disable once InconsistentNaming
   static void RunScenarios(Action action, TimeSpan singleThreadAllowedTime, TimeSpan multiThreadAllowedTime)
   {
      RunSingleThreadedScenario(action, singleThreadMaxTime: singleThreadAllowedTime);
      RunMultiThreadedScenario(action, multiThreadAllowedTime: multiThreadAllowedTime);
   }

   [Test] public void _010_Read_Unsafe________________________time_is_less_than_nanoseconds_SingleThreaded_06_MultiThreaded_01() =>
      RunScenarios(() => _guarded.Read_Unsafe(),
                   singleThreadAllowedTime: (6 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 60, unoptimized: 2.0),
                   multiThreadAllowedTime: (1 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 150, unoptimized: 2.0));

   [Test] public void _020_Read_Locked________________________time_is_less_than_nanoseconds_SingleThreaded_25_MultiThreaded_220() =>
      RunScenarios(() => _guarded.Read_Locked(),
                   singleThreadAllowedTime: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 18, unoptimized: 1.8),
                   multiThreadAllowedTime: (220 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.5));

   [Test] public void _031_Read_MonitorCE_Using_EnterLock______time_is_less_than_nanoseconds_SingleThreaded_50_MultiThreaded_450() =>
      RunScenarios(() => _guarded.Read_MonitorCE_Using_EnterLock(),
                   singleThreadAllowedTime: (50 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 55, unoptimized: 2.2),
                   multiThreadAllowedTime: (450 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 14, unoptimized:1.4));

   [Test] public void _032_Read_MonitorCE_Read________________time_is_less_than_nanoseconds_SingleThreaded_60_MultiThreaded_360() =>
      RunScenarios(() => _guarded.Read_MonitorCE_Read(),
                   singleThreadAllowedTime: (60 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 45.0, unoptimized: 2.2),
                   multiThreadAllowedTime: (360 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9.0, unoptimized:1.6));

   [Test] public void _050_Increment_Unsafe___________________________________time_is_less_than_nanoseconds_SingleThreaded_06_MultiThreaded_12() =>
      RunScenarios(() => _guarded.Increment_Unsafe(),
                   singleThreadAllowedTime: (6 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 80, unoptimized: 3.4),
                   multiThreadAllowedTime: (12 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 30, unoptimized: 2.4));

   [Test] public void _060_Increment_Locked___________________________________time_is_less_than_nanoseconds_SingleThreaded_35__MultiThreaded_300() =>
      RunScenarios(() => _guarded.Increment_Locked(),
                   singleThreadAllowedTime: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 15.0, unoptimized: 1.6),
                   multiThreadAllowedTime: (300 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.0));

   [Test] public void _070_Increment_MonitorCE_Using_EnterLock_________________time_is_less_than_nanoseconds_SingleThreaded_45__MultiThreaded_330() =>
      RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterLock(),
                   singleThreadAllowedTime: (45 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 60, unoptimized: 2.2),
                   multiThreadAllowedTime: (500 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8.0, unoptimized: 1.3));

   [Test] public void _073_Increment_MonitorCE_Update_________________________time_is_less_than_nanoseconds_SingleThreaded_80__MultiThreaded_340() =>
      RunScenarios(() => _guarded.Increment_MonitorCE_Update(),
                   singleThreadAllowedTime: (80 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 45, unoptimized: 1.8),
                   multiThreadAllowedTime: (460 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 12, unoptimized: 1.3));

}