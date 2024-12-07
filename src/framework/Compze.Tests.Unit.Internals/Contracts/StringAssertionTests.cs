using System;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.Contracts;

// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class StringAssertionTests : UniversalTestBase
{
   [Test] public void CorrectlyExtractsParameterNamesAndValues()
   {
      var notNullObject = new object();
      const string okString = "okString";
      const string emptyString = "";
      string nullString = null;
      const string spacesString = " ";
      const string tabsString = "   ";

      Invoking(() => Compze.Contracts.Assert.Argument.NotNull(nullString))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(nullString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNull(okString).NotNull(nullString).NotNull(notNullObject))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(nullString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNullOrEmpty(okString).NotNullOrEmpty(emptyString))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(emptyString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(spacesString))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(spacesString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(tabsString))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(tabsString));
   }
}
