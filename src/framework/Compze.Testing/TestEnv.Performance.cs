﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Compze.Logging;
using Compze.SystemCE;
using Compze.Testing.Performance;

namespace Compze.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
static partial class TestEnv
{
   public static TimeSpan EnvMultiply(this TimeSpan original, double instrumented = 1.0, double unoptimized = 1.0) =>
      original * EnvFactor(instrumented: instrumented, unoptimized: unoptimized);

   public static int EnvDivide(this int original, double instrumented = 1.0, double unoptimized = 1.0) =>
      (int)(original / EnvFactor(instrumented: instrumented, unoptimized: unoptimized));

   static double EnvFactor(double instrumented = 1.0, double unoptimized = 1.0)
   {
      // ReSharper disable once CompareOfFloatsByEqualityOperator
      if(Performance.IsInstrumented && instrumented != 1.0)
      {
         ConsoleCE.WriteLine($"Adjusting by {instrumented} for Instrumented Code");
         return instrumented;
      }

      if(Performance.AreOptimizationsDisabled)
      {
         ConsoleCE.WriteLine($"Adjusting by {unoptimized} for UnOptimized Code");
         return unoptimized;
      }

      ConsoleCE.WriteLine("Code is optimized. No adjustment made");
      return 1.0;
   }

#pragma warning disable CA1724 // Type names should not match namespaces
   public static class Performance
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      public static readonly bool AreOptimizationsDisabled = typeof(TestEnv).Assembly.GetCustomAttribute<DebuggableAttribute>()!.IsJITOptimizerDisabled;

      public static readonly bool IsInstrumented = CheckIfInstrumented();
      static bool CheckIfInstrumented()
      {
         var time = StopwatchCE.TimeExecution(action: () =>
                                              {
                                                 for(var i = 0; i < 100; i++)
                                                 {
                                                    // ReSharper disable once UnusedVariable
                                                    var something = i;
                                                 }
                                              },
                                              iterations: 500);

         return time.Total > 1.Milliseconds();
      }

      static readonly double MachineSlowness = DetectEnvironmentPerformanceAdjustment();
      const string MachineSlownessEnvironmentVariable = "COMPOSABLE_MACHINE_SLOWNESS";
      static double DetectEnvironmentPerformanceAdjustment()
      {
         var environmentOverride = Environment.GetEnvironmentVariable(MachineSlownessEnvironmentVariable);
         if(environmentOverride != null)
         {
            if(!double.TryParse(environmentOverride, NumberStyles.Any, CultureInfo.InvariantCulture, out var adjustment)) throw new Exception($"Environment variable har invalid value: {MachineSlownessEnvironmentVariable}. It should be parsable as a double.");

            return adjustment;
         }

         return 1.0;
      }

      public static TimeSpan? AdjustForMachineSlowness(TimeSpan? timespan) => timespan?.MultiplyBy(MachineSlowness);
      public static void LogMachineSlownessAdjustment()
      {
         // ReSharper disable once CompareOfFloatsByEqualityOperator
         if(MachineSlowness != 1.0) Console.WriteLine($"Adjusting allowed execution time with value {MachineSlowness} from environment variable {MachineSlownessEnvironmentVariable}");
      }
   }
}