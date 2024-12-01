using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests;

[TestFixture]public class TestEnvironmentConfigurationTest : UniversalTestBase
{
   //Todo: Verify that the environment setting seems sane. Also try splitting the environment variable into at least three. [IO,THREADED,SINGLETHREADED]_PERFORMANCE_FACTOR
   [Test, Ignore("todo")] public void Compze_performance_environment_variable_has_sane_value() => Assert.Inconclusive();
}