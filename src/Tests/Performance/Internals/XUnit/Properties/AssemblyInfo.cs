//Nothing in this project should run in parallel
//Note: For XUnit tests, the [Performance] attribute must be applied to each test class individually,
//as XUnit v2 does not support assembly-level trait attributes.
[assembly: NCrunch.Framework.EnableRdi(false)]
[assembly: NCrunch.Framework.Serial]
