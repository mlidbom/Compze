using AccountManagement.Domain;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;


namespace AccountManagement.Tests.Unit.Emails;

public class When_creating_a_new_email : UniversalTestBase
{
   public class An_InvalidEmailException_containing_the_email_is_thrown_if_email : UniversalTestBase
   {
      [Theory, MemberData(nameof(TestData.Emails.InvalidEmailsTestData), MemberType = typeof(TestData.Emails))]
      public void _(string? invalidEmail, string _)
      {
         var invalidEmailException = Invoking(() => Email.Parse(invalidEmail!))
                                    .Must().Throw<InvalidEmailException>().Which;

         if(!string.IsNullOrEmpty(invalidEmail))
         {
            invalidEmailException.Message.Must().Contain(invalidEmail);
         }
      }
   }

   [XF] public void An_InvalidEmailException_containing_the_string_null_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => Email.Parse(null!))
        .Must().Throw<InvalidEmailException>().Which
        .Message.Must().Contain("null");

   [XF] public void An_InvalidEmailException_containing_an_empty_quotation_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => Email.Parse(""))
        .Must().Throw<InvalidEmailException>().Which
        .Message.Must().Contain("''");
}
