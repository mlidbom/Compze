using System.Runtime.CompilerServices;
using Compze.Testing;
using NUnit.Framework;

[assembly: InternalsVisibleTo("AccountManagement.PerformanceTests.Internals")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
