using AccountManagement;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Global


namespace Compze.Utilities.Testing.XUnit.NCrutchErrorReproductions;

public class TheoriesOccasionallyCrashInNcrunchEvenWhenIsolated : UniversalTestBaseNCrunch
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
}
