using Compze.Contracts;
using Compze.Contracts.Deprecated;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Compze.Tests.Contracts;

[TestFixture]
public class ReturnTests : UniversalTestBase
{
   [Test]
   public void TestName()
   {
      Assert.Throws<ObjectIsNullContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(null));
      Assert.Throws<StringIsEmptyContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(""));
      Assert.Throws<StringIsWhitespaceContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(" ").Should().Be(""));
   }

   static string ReturnInputStringAndRefuseToReturnNull(string returnMe)
   {
      Contract.ReturnValue(returnMe).NotNullEmptyOrWhiteSpace();
      return Contract.Return(returnMe, assert => assert.NotNullEmptyOrWhiteSpace());
   }
}