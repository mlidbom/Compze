using System;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Contracts;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class LambdaBasedArgumentSpecsTests : UniversalTestBase
{
   [Test] public void CorrectlyExtractsParameterNamesAndValues()
   {
      var notNullObject = new object();
      var okString = "okString";
      var emptyString = "";
      string nullString = null;
      Invoking(() => Compze.Contracts.Assert.Argument.NotNull(nullString))
        .Should().Throw<ArgumentNullException>()
        .Which.Message.Should().Contain(nameof(nullString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNull(okString).NotNull(nullString).NotNull(notNullObject))
        .Should().Throw<ArgumentNullException>()
        .Which.Message.Should().Contain(nameof(nullString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNullOrEmpty(okString).NotNullOrEmpty(emptyString))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(emptyString));
   }
}