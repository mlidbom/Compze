using System;
using NUnit.Framework.Internal;

namespace Composable.Testing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LongRunningAttribute : Attribute,
                                    NUnit.Framework.Interfaces.IApplyToTest
{
   public void ApplyToTest(Test test)
      => test.Properties.Add(PropertyNames.Category, "LongRunning");
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly)]
public sealed class LevelOfParallelismCEAttribute : Attribute,
                                             NUnit.Framework.Interfaces.IApplyToTest
{
   public void ApplyToTest(Test test)
      => test.Properties.Add(PropertyNames.LevelOfParallelism, Math.Max(Environment.ProcessorCount / 3, 4)); //Math.Max(Environment.ProcessorCount / 3, 4)
}