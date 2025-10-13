using System;
using Compze.Utilities.SystemCE;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

public class NUnitTestBase : UniversalTestBase
{
   [TearDown] public void SurfaceAnyUncatchableExceptions() => UncatchableExceptionsGatherer.ConsumeAndThrowAnyExceptionsGathered();
}
