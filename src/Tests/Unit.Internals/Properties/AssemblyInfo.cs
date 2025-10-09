using Compze.TestInfrastructure;
using Compze.TestInfrastructure.NUnit;
using NUnit.Framework;

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
