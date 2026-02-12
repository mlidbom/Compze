////Nothing in this project should run in parallel
#if !NCRUNCH
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
#endif

//[assembly: NCrunch.Framework.EnableRdi(false)]
//[assembly: NCrunch.Framework.Serial]
[assembly: Compze.Tests.Infrastructure.XUnit.PerformanceAttribute]