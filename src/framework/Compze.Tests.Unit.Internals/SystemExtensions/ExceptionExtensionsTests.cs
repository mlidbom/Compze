using System;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.SystemExtensions;

[TestFixture] public class ExceptionExtensionsTests : UniversalTestBase
{
   Exception _originalException;
   Exception _firstNestingException;
   Exception _secondNestingException;

   [SetUp] public void Setup()
   {
      _originalException = new Exception("Root cause exception");
      _firstNestingException = new Exception("nested once", _originalException);
      _secondNestingException = new Exception("nested twice", _firstNestingException);
   }

   [Test] public void GetAllExceptionsInStackShouldReturnAllNestedExceptionsInOrderFromRootToMostNestedException()
   {
      var expected = EnumerableCE.Create(_secondNestingException, _firstNestingException, _originalException);
      var actual = _secondNestingException.GetAllExceptionsInStack();
      actual.Should().Equal(expected);
   }

   [Test] public void GetRootCauseExceptionShouldReturnMostNestedException() => _secondNestingException.GetRootCauseException().Should().Be(_originalException);
}
