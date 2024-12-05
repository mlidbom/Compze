using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;
// ReSharper disable ExpressionIsAlwaysNull

namespace Compze.Tests.Contracts;

[TestFixture] public class ReturnTests : UniversalTestBase
{
   [Test] public void TestName()
   {
      int? nullObject = null;
      int? emptyObject = 0;
      Invoking(() => Result.ReturnNotNull(nullObject)).Should().Throw<ResultAssertionException>().Which.Message.Should().Contain(nameof(nullObject));
      Invoking(() => Result.ReturnNotNullOrDefault(emptyObject)).Should().Throw<ResultAssertionException>().Which.Message.Should().Contain(nameof(emptyObject));
   }
}
