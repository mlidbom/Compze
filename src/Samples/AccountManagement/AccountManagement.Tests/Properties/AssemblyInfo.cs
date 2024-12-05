using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Testing;
using NUnit.Framework;


[assembly: AssemblyVersion("1.0.0.0")]
[assembly: InternalsVisibleTo("AccountManagement.PerformanceTests.Internals")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
