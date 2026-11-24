using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Compze.Tests.Benchmarks;

///<summary>
/// Runs BenchmarkDotNet benchmarks of Compze's code: what an operation costs in time and in allocated memory, measured precisely enough<br/>
/// to compare two versions of code that takes nanoseconds. Run it in Release, choosing the benchmarks by name:<br/>
/// <c>dotnet run -c Release --project test/Compze.Tests.Benchmarks -- --filter *LazyCE*</c>. Without a filter it asks which to run.
///</summary>
///<remarks>
/// The benchmarks run in this process, not in one BenchmarkDotNet builds for each: BenchmarkDotNet 0.15.8, the latest release on<br/>
/// 2026-11-24, doesn't recognize .NET 11, and fails validating a benchmark it would build a project for. A run in this process measures<br/>
/// the same code under the same JIT, with less isolation from what ran before it in the process.
///</remarks>
static class Program
{
   static void Main(string[] args) =>
      BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                       .Run(args, DefaultConfig.Instance.AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance)));
}
