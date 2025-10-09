using System;
using NUnit.Framework.Internal;
using NUnit.Framework.Interfaces;
using Compze.TestInfrastructure.NUnit;

namespace Compze.TestInfrastructure.NUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LongRunningAttribute : Attribute,
                                    IApplyToTest
{
   public void ApplyToTest(Test test)
      => test.Properties.Add(PropertyNames.Category, "LongRunning");
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly)]
public sealed class LevelOfParallelismCEAttribute : Attribute,
                                             IApplyToTest
{
   public void ApplyToTest(Test test)
      => test.Properties.Add(PropertyNames.LevelOfParallelism, Math.Max(Environment.ProcessorCount / 3, 4)); //Math.Max(Environment.ProcessorCount / 3, 4)
}