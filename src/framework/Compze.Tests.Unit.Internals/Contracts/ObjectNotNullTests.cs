using System;
using System.Collections.Generic;
using Compze.Contracts;
using Compze.Contracts.Deprecated;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Compze.Tests.Contracts;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable ExpressionIsAlwaysNull
[TestFixture]
public class ObjectNotNullTests : UniversalTestBase
{
   [Test]
   public void ThrowsObjectNullExceptionForNullValues()
   {
      InspectionTestHelper.BatchTestInspection<ObjectIsNullContractViolationException, object>(
         inspected => inspected.NotNull(),
         badValues: new List<object> {null, null},
         goodValues: new List<object> {new(), "", Guid.NewGuid()});


      string nullString = null;
      var anObject = new object();

      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull());
      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => anObject, () => nullString).NotNull());
      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull())
            .Message.Should().Contain("nullString");

      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Invariant(nullString, nameof(nullString)).NotNull());
      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Invariant(anObject, nameof(anObject), nullString, nameof(nullString)).NotNull());
      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Invariant(nullString, nameof(nullString)).NotNull())
            .Message.Should().Contain("nullString");
   }

   [Test]
   public void UsesArgumentNameForExceptionmessage()
   {
      string nullString = null;

      Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull())
            .Message.Should().Contain("nullString");
   }
}

// ReSharper restore ConvertToConstant.Local
// ReSharper restore ExpressionIsAlwaysNull