using System.Runtime.CompilerServices;
using Compze.Testing;
using NUnit.Framework;

[assembly: InternalsVisibleTo("Compze.Tests.Performance.Internals")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
