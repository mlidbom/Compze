﻿[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

#if !NCRUNCH
[assembly: NUnit.Framework.Parallelizable(NUnit.Framework.ParallelScope.None)]
#endif

//Nothing in this project should run in parallel
[assembly: NCrunch.Framework.EnableRdi(false)]
[assembly:NCrunch.Framework.Serial, NUnit.Framework.Category("Performance")]