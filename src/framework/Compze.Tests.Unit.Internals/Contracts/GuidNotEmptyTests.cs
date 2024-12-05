using System;
using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Contracts;

[TestFixture] public class GuidNotEmptyTests : UniversalTestBase
{
   [Test] public void NotEmptyThrowsArgumentExceptionForEmptyGuid()
   {
      var emptyGuid = Guid.Empty;

      Invoking(() => Compze.Contracts.Assert.Result.NotDefault(emptyGuid))
        .Should().Throw<InvalidResultException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));

      Invoking(() => Compze.Contracts.Assert.Argument.NotDefault(emptyGuid))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));

      Invoking(() => Compze.Contracts.Assert.State.NotDefault(emptyGuid))
        .Should().Throw<InvalidOperationException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));

      Invoking(() => Compze.Contracts.Assert.Invariant.NotDefault(emptyGuid))
        .Should().Throw<InvariantViolatedException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));
   }
}
