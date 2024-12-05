using System.Reflection;
using Compze.Testing;
using NUnit.Framework;

[assembly: AssemblyVersion("1.0.0.0")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
