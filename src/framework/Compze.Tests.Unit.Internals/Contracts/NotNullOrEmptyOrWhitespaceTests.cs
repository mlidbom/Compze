using System;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Contracts;
// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class NotNullOrEmptyOrWhitespaceTests : UniversalTestBase
{
   [Test] public void ThrowsArgumentNullForNullArguments()
   {
      string aNullString = null;
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(aNullString!))
        .Should().Throw<ArgumentNullException>()
        .Which.Message.Should().Contain(nameof(aNullString));
   }

   [Test] public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings() =>
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(string.Empty))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(string.Empty));

   [Test] public void ThrowsArgumentExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
   {
      const string spaceString = " ";
      var tabString = "\t";
      var lineBreakString = "\n";
      var newLineString = "\r\n";
      var environmentNewLineString = Environment.NewLine;
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(spaceString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(spaceString));
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(tabString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(tabString));
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(lineBreakString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(lineBreakString));
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(newLineString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(newLineString));
      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(environmentNewLineString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(environmentNewLineString));

      Invoking(() => Compze.Contracts.Assert.Argument.NotNullEmptyOrWhitespace(environmentNewLineString).NotNullEmptyOrWhitespace(spaceString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(environmentNewLineString));
   }
}
