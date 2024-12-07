using System;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static Compze.Contracts.Assert;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.Contracts;
// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class NotNullOrEmptyOrWhitespaceTests : UniversalTestBase
{
   [Test] public void ThrowsArgumentNullForNullArguments()
   {
      string aNullString = null;
      Invoking(() => Argument.NotNullEmptyOrWhitespace(aNullString!))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(aNullString));
   }

   [Test] public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings() =>
      Invoking(() => Argument.NotNullEmptyOrWhitespace(string.Empty))
        .Should().Throw<ArgumentException>()
        .Which.Message.Should().Contain(nameof(string.Empty));

   [Test] public void ThrowsArgumentExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
   {
      const string spaceString = " ";
      const string tabString = "\t";
      const string lineBreakString = "\n";
      const string newLineString = "\r\n";
      var environmentNewLineString = Environment.NewLine;
      Invoking(() => Argument.NotNullEmptyOrWhitespace(spaceString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(spaceString));
      Invoking(() => Argument.NotNullEmptyOrWhitespace(tabString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(tabString));
      Invoking(() => Argument.NotNullEmptyOrWhitespace(lineBreakString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(lineBreakString));
      Invoking(() => Argument.NotNullEmptyOrWhitespace(newLineString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(newLineString));
      Invoking(() => Argument.NotNullEmptyOrWhitespace(environmentNewLineString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(environmentNewLineString));

      Invoking(() => Argument.NotNullEmptyOrWhitespace(environmentNewLineString).NotNullEmptyOrWhitespace(spaceString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(environmentNewLineString));
   }
}
