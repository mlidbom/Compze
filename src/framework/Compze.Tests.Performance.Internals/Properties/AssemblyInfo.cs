//Nothing in this project should run in parallel
[assembly: NCrunch.Framework.EnableRdi(false)]
[assembly:NCrunch.Framework.Serial, NUnit.Framework.Category("Performance")]
[assembly:NUnit.Framework.NonParallelizable]
