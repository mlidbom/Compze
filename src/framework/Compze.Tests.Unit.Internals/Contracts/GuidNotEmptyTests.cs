using System;
using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static Compze.Contracts.Assert;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.Contracts;

[TestFixture] public class GuidNotEmptyTests : UniversalTestBase
{
   [Test] public void NotEmptyThrowsArgumentExceptionForEmptyGuid()
   {
      var emptyGuid = Guid.Empty;

      Invoking(() => Result.NotDefault(emptyGuid))
        .Should().Throw<InvalidResultException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));

      Invoking(() => Argument.NotDefault(emptyGuid))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));

      Invoking(() => State.NotDefault(emptyGuid))
        .Should().Throw<InvalidOperationException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));

      Invoking(() => Invariant.NotDefault(emptyGuid))
        .Should().Throw<InvariantViolatedException>()
        .Which.Message.Should().Contain(nameof(emptyGuid));
   }
}
