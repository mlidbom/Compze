using System;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;

namespace Compze.Tests.Contracts;

[TestFixture] public class ObjectNotDefaultTests : UniversalTestBase
{
   [Test] public void ThrowsArgumentExceptionIfAnyValueIsDefault()
   {
      var myDefaultStructure = new MyStructure();
      const int zero = 0;

      Invoking(() => Argument.NotDefault(zero)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(zero));
      Invoking(() => Argument.NotDefault(myDefaultStructure)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(myDefaultStructure));

      var myNonDefaultStructure = new MyStructure
                                  {
                                     Value = 2
                                  };

      Argument.NotDefault(myNonDefaultStructure);
   }

   struct MyStructure
   {
      // ReSharper disable once UnusedAutoPropertyAccessor.Local
      public int Value { get; set; }
   }
}
