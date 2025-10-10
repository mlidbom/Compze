using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.NUnit;
using NUnit.Framework;

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
