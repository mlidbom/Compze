using System;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;

namespace Compze.Tests.Unit.Internals.Contracts;

[TestFixture] public class ObjectNotDefaultTests : UniversalTestBase
{
   [Test] public void ThrowsArgumentExceptionIfAnyValueIsDefault()
   {
      var myDefaultStructure = new DateTime();
      const int zero = 0;

      Invoking(() => Argument.NotDefault(zero)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(zero));
      Invoking(() => Argument.NotDefault(myDefaultStructure)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(myDefaultStructure));

      var myNonDefaultStructure = DateTime.Now;

      Argument.NotDefault(myNonDefaultStructure);
   }
}
