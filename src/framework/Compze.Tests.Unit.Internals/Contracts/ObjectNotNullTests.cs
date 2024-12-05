using System;
using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;

namespace Compze.Tests.Contracts;

// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class ObjectNotNullTests : UniversalTestBase
{
   [Test] public void ThrowsObjectNullExceptionForNullValues()
   {
      string nullString = null;
      var anObject = new object();

      Invoking(() => Invariant.NotNull(nullString)).Should().Throw<InvariantViolatedException>().Which.Message.Should().Contain(nameof(nullString));
      Invoking(() => Invariant.NotNull(anObject).NotNull(nullString)).Should().Throw<InvariantViolatedException>().Which.Message.Should().Contain(nameof(nullString));
      Invoking(() => Invariant.NotNull(nullString)).Should().Throw<InvariantViolatedException>().Which.Message.Should().Contain(nameof(nullString));

      Invoking(() => Argument.NotNull(nullString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(nullString));
      Invoking(() => Argument.NotNull(anObject).NotNull(nullString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(nullString));
   }
}
