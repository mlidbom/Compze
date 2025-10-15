using Compze.Tests.Infrastructure.NUnit;
using NUnit.Framework;

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
