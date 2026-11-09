////Nothing in this project should run in parallel
#if !NCRUNCH

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
#endif

//[assembly: NCrunch.Framework.EnableRdi(false)]
//[assembly: NCrunch.Framework.Serial]
[assembly: Compze.Tests.Infrastructure.XUnit.PerformanceAttribute]

