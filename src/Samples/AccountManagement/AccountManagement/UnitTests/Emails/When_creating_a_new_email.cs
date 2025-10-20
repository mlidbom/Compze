using AccountManagement.Domain;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;


namespace AccountManagement.Tests.Unit.Emails;

public class When_creating_a_new_email : UniversalTestBase
{
   public class An_InvalidEmailException_containing_the_email_is_thrown_if_email : UniversalTestBase
   {
      [Theory, MemberData(nameof(TestData.Emails.InvalidEmailsTestData), MemberType = typeof(TestData.Emails))]
      public void _(string? invalidEmail, string _)
      {
         var invalidEmailException = Invoking(() => Email.Parse(invalidEmail!))
                                    .Should().Throw<InvalidEmailException>().Which;

         if(!string.IsNullOrEmpty(invalidEmail))
         {
            invalidEmailException.Message.Should().Contain(invalidEmail);
         }
      }
   }

   [XF] public void An_InvalidEmailException_containing_the_string_null_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => Email.Parse(null!))
        .Should().Throw<InvalidEmailException>().Which
        .Message.Should().Contain("null");

   [XF] public void An_InvalidEmailException_containing_an_empty_quotation_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => Email.Parse(""))
        .Should().Throw<InvalidEmailException>().Which
        .Message.Should().Contain("''");
}
