using Compze.Contracts;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;

namespace Compze.Tests.Contracts;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class ObjectNotNullOrDefaultTests : UniversalTestBase
{
   [Test] public void ThrowsArgumentNullExceptionIfAnyValueIsNull()
   {
      var anObject = new object();
      object nullObject = null;
      string? nullString = null;

      Invoking(() => Invariant.NotNullOrDefault(nullObject)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(nullObject));
      Invoking(() => Invariant.NotNullOrDefault(nullString)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(nullString));
      Invariant.NotNullOrDefault(anObject);
   }

   [Test] public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
   {
      var emptyString = "";
      var zero = 0;
      var defaultMyStructure = new MyStructure();

      Invoking(() => Invariant.NotNullOrDefault(zero)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(zero));
      Invoking(() => Invariant.NotNullOrDefault(defaultMyStructure)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(defaultMyStructure));
   }

   struct MyStructure
   {
      // ReSharper disable NotAccessedField.Local
#pragma warning disable IDE0052 //Review OK: This member is used through reflection.
      readonly int _value;
#pragma warning restore IDE0052 // Remove unread private members
      // ReSharper restore NotAccessedField.Local
   }
}

// ReSharper restore ConvertToConstant.Local
// ReSharper restore ExpressionIsAlwaysNull
