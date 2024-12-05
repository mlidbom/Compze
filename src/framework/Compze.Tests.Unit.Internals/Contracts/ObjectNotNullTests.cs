using System;
using System.Collections.Generic;
using Compze.Contracts;
using Compze.Contracts.Deprecated;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using static Compze.Contracts.Assert;

namespace Compze.Tests.Contracts;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable ExpressionIsAlwaysNull
[TestFixture] public class ObjectNotNullTests : UniversalTestBase
{
   [Test] public void ThrowsObjectNullExceptionForNullValues()
   {
      InspectionTestHelper.BatchTestInspection<ObjectIsNullContractViolationException, object>(
         inspected => inspected.NotNull(),
         badValues: new List<object> { null, null },
         goodValues: new List<object> { new(), "", Guid.NewGuid() });

      string nullString = null;
      var anObject = new object();

      Invoking(() => Invariant.NotNull(nullString)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(nullString));
      Invoking(() => Invariant.NotNull(anObject).NotNull(nullString)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(nullString));
      Invoking(() => Invariant.NotNull(nullString)).Should().Throw<InvariantAssertionException>().Which.Message.Should().Contain(nameof(nullString));

      Invoking(() => Argument.NotNull(nullString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(nullString));
      Invoking(() => Argument.NotNull(anObject).NotNull(nullString)).Should().Throw<ArgumentException>().Which.Message.Should().Contain(nameof(nullString));
   }
}

// ReSharper restore ConvertToConstant.Local
// ReSharper restore ExpressionIsAlwaysNull
