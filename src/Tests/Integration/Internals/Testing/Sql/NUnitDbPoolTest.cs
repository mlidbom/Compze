using Compze.Tests.Common.Testing.Sql;
using Compze.Tests.Infrastructure.NUnit;
using NUnit.Framework;

namespace Compze.Tests.Integration.Internals.Testing.Sql;

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public abstract class NUnitDbPoolTest(string pluggableComponentsCombination) : DbPoolTestBase
{}