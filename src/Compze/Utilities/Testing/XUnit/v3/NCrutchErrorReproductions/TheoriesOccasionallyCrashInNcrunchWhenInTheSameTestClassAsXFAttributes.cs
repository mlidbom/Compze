using AccountManagement;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;


namespace Compze.Utilities.Testing.XUnit.NCrutchErrorReproductions;

public class When_creating_a_new_email : UniversalTestBaseNCrunch
{
   public class An_InvalidEmailException_containing_the_email_is_thrown_if_email : UniversalTestBaseNCrunch
   {
      [Theory, MemberData(nameof(TestData.Emails.InvalidEmailsTestData), MemberType = typeof(TestData.Emails))]
      public void _(string? invalidEmail, string _)
      {

         if(!string.IsNullOrEmpty(invalidEmail))
         {
         }
      }
   }

   [XF] public void An_InvalidEmailException_containing_the_string_null_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => throw new Exception())
        .Should().Throw<Exception>();

   [XF] public void An_InvalidEmailException_containing_an_empty_quotation_is_thrown_if_the_string_passed_is_null()
      => Invoking(() => throw new Exception())
        .Should().Throw<Exception>();
}
