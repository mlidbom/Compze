﻿using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;
// ReSharper disable ExpressionIsAlwaysNull

namespace Compze.Tests.Unit.Internals.Contracts;

[TestFixture] public class ReturnTests : UniversalTestBase
{
   [Test] public void TestName()
   {
      int? nullObject = null;
      int? emptyObject = 0;
      Invoking(() => Result.ReturnNotNull(nullObject)).Should().Throw<InvalidResultException>().Which.Message.Should().Contain(nameof(nullObject));
      Invoking(() => Result.ReturnNotNullOrDefault(emptyObject)).Should().Throw<InvalidResultException>().Which.Message.Should().Contain(nameof(emptyObject));
   }
}
