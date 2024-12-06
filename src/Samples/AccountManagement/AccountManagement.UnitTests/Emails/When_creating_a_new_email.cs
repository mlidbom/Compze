using AccountManagement.Domain;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace AccountManagement.UnitTests.Emails;

[TestFixture] public class When_creating_a_new_email : UniversalTestBase
{
   [TestFixture] public class An_InvalidEmailException_containing_the_email_is_thrown_if_email : UniversalTestBase
   {
      [Test, TestCaseSource(typeof(TestData.Emails), nameof(TestData.Emails.InvalidEmailsTestData))]
      public void _(string invalidEmail) //The _ name is a hack that colludes with the test data source to manage to get the ReSharper, VS, and NCrunch test runners to all show a descriptive name based on the test source data for each case
      {
         var invalidEmailException = Invoking(() => Email.Parse(invalidEmail))
                                    .Should().Throw<InvalidEmailException>().Which;

         if(!string.IsNullOrEmpty(invalidEmail))
         {
            invalidEmailException.Message.Should().Contain(invalidEmail);
         }
      }
   }

   [Test] public void An_InvalidEmailException_containing_the_string_null_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => Email.Parse(null!))
        .Should().Throw<InvalidEmailException>().Which
        .Message.Should().Contain("null");

   [Test] public void An_InvalidEmailException_containing_an_empty_quotation_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => Email.Parse(""))
        .Should().Throw<InvalidEmailException>().Which
        .Message.Should().Contain("''");
}
