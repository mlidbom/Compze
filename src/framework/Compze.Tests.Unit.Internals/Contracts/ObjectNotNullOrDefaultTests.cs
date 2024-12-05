using System;
using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;

namespace Compze.Tests.Contracts;

// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class ObjectNotNullOrDefaultTests : UniversalTestBase
{
   [Test] public void ThrowsArgumentNullExceptionIfAnyValueIsNull()
   {
      int? anObject = 1;
      int? nullObject = null;

      Invoking(() => Invariant.NotNullOrDefault(nullObject)).Should().Throw<InvariantViolatedException>().Which.Message.Should().Contain(nameof(nullObject));
      Invariant.NotNullOrDefault(anObject);
   }

   [Test] public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
   {
      int? zero = 0;
      DateTime? defaultMyStructure = new DateTime();

      Invoking(() => Invariant.NotNullOrDefault(zero)).Should().Throw<InvariantViolatedException>().Which.Message.Should().Contain(nameof(zero));
      Invoking(() => Invariant.NotNullOrDefault(defaultMyStructure)).Should().Throw<InvariantViolatedException>().Which.Message.Should().Contain(nameof(defaultMyStructure));
   }
}