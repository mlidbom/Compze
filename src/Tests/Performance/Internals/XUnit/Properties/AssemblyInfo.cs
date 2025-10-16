
//Nothing in this project should run in parallel
[assembly: NCrunch.Framework.EnableRdi(false)]
[assembly: NCrunch.Framework.Serial]

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
