using System.Threading.Tasks;
using Compze.Tests.Common.Sql.DocumentDb;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Common.NUnit.Sql.DocumentDb;

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public abstract class NUnitDocumentDbTestsBase(string pluggableComponentsCombination) : DocumentDbTestsBase
{
   [SetUp] public override void Setup() => base.Setup();
   [TearDown] public override async Task TearDownTask() => await base.TearDownTask();
}
